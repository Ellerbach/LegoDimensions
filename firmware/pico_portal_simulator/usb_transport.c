#include "usb_transport.h"

#include <string.h>

#include "portal_protocol.h"
#include "pico/critical_section.h"
#include "pico/time.h"
#include "tusb.h"
#include "usb_descriptors.h"
#include "xsm3_relay.h"

static portal_usb_variant_t active_variant;
static usb_transport_status_t transport_status;
static critical_section_t transport_lock;
static uint8_t pending_frame[32];
static bool frame_pending;
static bool announce_pending;
static uint8_t gip_sequence;
static uint8_t hid_rx[32];
static volatile bool hid_rx_pending;

static void record_trace(const uint8_t *report, uint8_t length, bool portal_to_xbox) {
    if (transport_status.trace_count >= USB_TRACE_CAPACITY) {
        memmove(&transport_status.trace[0], &transport_status.trace[1],
            sizeof(transport_status.trace[0]) * (USB_TRACE_CAPACITY - 1));
        transport_status.trace_count--;
    }
    usb_trace_entry_t *entry = &transport_status.trace[transport_status.trace_count++];
    entry->timestamp_ms = to_ms_since_boot(get_absolute_time());
    entry->portal_to_xbox = portal_to_xbox;
    entry->length = length;
    memcpy(entry->data, report, length);
}

static void record_rx(const uint8_t *report, uint8_t length) {
    critical_section_enter_blocking(&transport_lock);
    transport_status.rx_transfers++;
    transport_status.last_rx_length = length;
    memcpy(transport_status.last_rx, report, length);
    record_trace(report, length, false);
    critical_section_exit(&transport_lock);
}

static void record_tx(const uint8_t *report, uint8_t length, bool success) {
    critical_section_enter_blocking(&transport_lock);
    if (success) {
        transport_status.tx_transfers++;
        transport_status.last_tx_length = length;
        memcpy(transport_status.last_tx, report, length);
        record_trace(report, length, true);
    } else {
        transport_status.tx_failures++;
    }
    critical_section_exit(&transport_lock);
}

static bool unwrap(const uint8_t *report, uint8_t length, uint8_t frame[32]) {
    memset(frame, 0, 32);
    switch (active_variant) {
        case PORTAL_USB_XBOX_360:
            if (length != 32 || report[0] != 0x0b || report[1] != 0x16) {
                return false;
            }
            memcpy(frame, report + 2, 30);
            return true;
        case PORTAL_USB_STANDARD:
            if (length != 32) return false;
            memcpy(frame, report, 32);
            return true;
        case PORTAL_USB_XBOX_ONE:
            if (length != 36 || report[0] != 0x21 || report[3] != 0x20) return false;
            memcpy(frame, report + 4, 32);
            return true;
        default:
            return false;
    }
}

static uint8_t wrap(const uint8_t frame[32], uint8_t report[USB_PACKET_MAX]) {
    memset(report, 0, USB_PACKET_MAX);
    switch (active_variant) {
        case PORTAL_USB_XBOX_360:
            report[0] = 0x0b;
            report[1] = 0x16;
            memcpy(report + 2, frame, 30);
            return 32;
        case PORTAL_USB_STANDARD:
            memcpy(report, frame, 32);
            return 32;
        case PORTAL_USB_XBOX_ONE:
            report[0] = 0x21;
            report[2] = gip_sequence++;
            if (gip_sequence == 0) gip_sequence = 1;
            report[3] = 0x20;
            memcpy(report + 4, frame, 32);
            return 36;
        default:
            return 0;
    }
}

static bool transport_write(const uint8_t *report, uint8_t length) {
    bool success;
    if (active_variant == PORTAL_USB_STANDARD) {
        success = tud_hid_n_ready(0) && tud_hid_n_report(0, 0, report, length);
    } else {
        uint32_t written = tud_vendor_write(report, length);
        tud_vendor_flush();
        success = written == length;
    }
    record_tx(report, length, success);
    return success;
}

static bool send_frame(const uint8_t frame[32]) {
    uint8_t report[USB_PACKET_MAX];
    uint8_t length = wrap(frame, report);
    if (!tud_mounted() || length == 0) {
        return false;
    }
    return transport_write(report, length);
}

static bool send_announce(void) {
    uint8_t report[32] = {
        0x02, 0x20, 0x01, 0x1c,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x6f, 0x0e, 0x41, 0x01,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    };
    return transport_write(report, sizeof(report));
}

void usb_transport_init(portal_usb_variant_t variant) {
    active_variant = variant;
    memset(&transport_status, 0, sizeof(transport_status));
    frame_pending = false;
    announce_pending = variant == PORTAL_USB_XBOX_ONE;
    gip_sequence = 2;
    hid_rx_pending = false;
    critical_section_init(&transport_lock);
    usb_descriptors_set_variant(variant);
    if (variant == PORTAL_USB_XBOX_360) xsm3_relay_init();
    tusb_init();
}

portal_usb_variant_t usb_transport_variant(void) {
    return active_variant;
}

const char *usb_transport_variant_name(portal_usb_variant_t variant) {
    switch (variant) {
        case PORTAL_USB_XBOX_360: return "xbox360";
        case PORTAL_USB_XBOX_ONE: return "xboxone";
        case PORTAL_USB_STANDARD: return "standard";
        default: return "unknown";
    }
}

bool usb_transport_ready(void) {
    return tud_mounted();
}

void usb_transport_get_status(usb_transport_status_t *status) {
    critical_section_enter_blocking(&transport_lock);
    *status = transport_status;
    status->mounted = tud_mounted();
    critical_section_exit(&transport_lock);
}

void usb_transport_task(void) {
    tud_task();
    if (active_variant == PORTAL_USB_XBOX_360) xsm3_relay_task();

    if (announce_pending && tud_mounted()) {
        if (send_announce()) announce_pending = false;
        return;
    }

    if (frame_pending) {
        bool writable = active_variant == PORTAL_USB_STANDARD ?
            tud_hid_n_ready(0) : tud_vendor_write_available() >=
                (active_variant == PORTAL_USB_XBOX_ONE ? 36 : 32);
        if (writable && send_frame(pending_frame)) {
            frame_pending = false;
        }
        return;
    }

    if (portal_protocol_next_event(pending_frame)) {
        frame_pending = true;
        return;
    }

    uint32_t available = active_variant == PORTAL_USB_STANDARD ?
        (hid_rx_pending ? sizeof(hid_rx) : 0) : tud_vendor_available();
    if (available > 0) {
        uint8_t report[USB_PACKET_MAX];
        uint32_t length;
        if (active_variant == PORTAL_USB_STANDARD) {
            memcpy(report, hid_rx, sizeof(hid_rx));
            length = sizeof(hid_rx);
            hid_rx_pending = false;
        } else {
            uint32_t read_size = active_variant == PORTAL_USB_XBOX_360 ? 32 : sizeof(report);
            length = tud_vendor_read(report, read_size);
        }
        if (length == 0) return;
        record_rx(report, (uint8_t)length);

        // 01 03 xx is the Xbox player-light initialization command. Its
        // completed 3-byte OUT transfer is acknowledged by USB when read.
        if (active_variant == PORTAL_USB_XBOX_360 && length == 3 &&
                report[0] == 0x01 && report[1] == 0x03) {
            critical_section_enter_blocking(&transport_lock);
            transport_status.xinput_commands++;
            critical_section_exit(&transport_lock);
            return;
        }

        uint8_t frame[32];
        if (unwrap(report, (uint8_t)length, frame) &&
                frame[0] == 0x55) {
            critical_section_enter_blocking(&transport_lock);
            transport_status.lego_commands++;
            if (frame[2] == 0xb0) transport_status.wake_commands++;
            critical_section_exit(&transport_lock);
            frame_pending = portal_protocol_handle_frame(frame, pending_frame);
        }
    }
}

bool tud_vendor_control_xfer_cb(uint8_t rhport, uint8_t stage,
    tusb_control_request_t const *request) {
    if (active_variant == PORTAL_USB_XBOX_360) {
        return xsm3_relay_control_xfer(rhport, stage, request);
    }
    return usb_descriptors_control_xfer(rhport, stage, request);
}

uint16_t tud_hid_get_report_cb(uint8_t instance, uint8_t report_id,
    hid_report_type_t report_type, uint8_t *buffer, uint16_t requested_length) {
    (void)instance; (void)report_id; (void)report_type; (void)buffer; (void)requested_length;
    return 0;
}

void tud_hid_set_report_cb(uint8_t instance, uint8_t report_id,
    hid_report_type_t report_type, uint8_t const *buffer, uint16_t bufsize) {
    (void)instance; (void)report_id; (void)report_type;
    if (active_variant == PORTAL_USB_STANDARD && bufsize == sizeof(hid_rx) && !hid_rx_pending) {
        memcpy(hid_rx, buffer, sizeof(hid_rx));
        hid_rx_pending = true;
    }
}
