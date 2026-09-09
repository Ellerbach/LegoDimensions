#include "xsm3_relay.h"

#include <stdio.h>
#include <string.h>

#include "device/dcd.h"
#include "pico/critical_section.h"
#include "pico/rand.h"
#include "pico/time.h"

#include "xsm3.h"
#include "xsm3_debug_log.h"

#define XSM3_INTERFACE 3
// VID/PID of the genuine LEGO Dimensions Xbox 360 portal. NOTE: this is the
// XSM3 identification packet's own embedded product ID, confirmed via a
// real capture -- NOT the same as the outer USB device descriptor's PID
// (0xFA01), which is a separate field entirely.
#define XSM3_VID 0x24C6
#define XSM3_PID 0x5000
// CONFIRMED ROOT CAUSE (via real-hardware testing): a real portal reports
// category 0x82 on the wire, but xsm3_root_key_0x23/0x24 (used to derive
// the per-console response-encryption keys) are only publicly known for
// the "1st party controller" keyvault slot -- the portal-specific keys
// are unpublished. Presenting as a generic controller (0x02, the untouched
// upstream placeholder) makes the console validate our response against
// the controller keyvault slot, which matches these keys. Confirmed working
// end-to-end: console completes 0x83/0x84/0x87 and the game's own LEGO
// protocol traffic starts flowing.
#define XSM3_CATEGORY 0x02

// Two-byte state values returned for request 0x86 ("done?" poll). A real
// capture showed the portal answers 1 on the first poll after a 0x82/0x87
// cycle, then 2 on every poll after that -- not a constant "always
// complete" -- and the console only proceeds to 0x83 once it's seen that
// 1-then-2 transition.
static const uint8_t STATE_PENDING[2] = {0x01, 0x00};
static const uint8_t STATE_COMPLETE[2] = {0x02, 0x00};
static uint8_t xsm3_poll_count;

static uint8_t control_out_buffer[64];
static uint16_t xsm3_response_length;
static bool xsm3_initialised;
// Mutable copy of xsm3_id_data_ms_controller (upstream's copy is const) with
// our own serial/VID/PID patched in.
static uint8_t identification_data[0x1D];
static xsm3_relay_status_t relay_status;
static xsm3_trace_entry_t trace_entries[XSM3_TRACE_CAPACITY];
static uint8_t trace_start;
static uint8_t trace_count;
static uint32_t trace_sequence;
static uint32_t trace_transaction;
static critical_section_t trace_lock;
// Genuine hardware serial (confirmed via ToysToLifeLib's X360_SERIAL/xinput
// constants), used both here and as usb_descriptors.c's iSerialNumber string.
static uint8_t xinput_capability_response[4] = {0x03, 0x10, 0x8E, 0x28};
static uint8_t xinput_descriptor_response[20] = {0x00, 0x14};
static uint8_t xinput_vibration_response[8] = {0x00, 0x08};

static void trace_event(xsm3_trace_event_t event, xsm3_trace_status_t status,
    int16_t status_code, uint32_t transaction,
    tusb_control_request_t const *request, const uint8_t *data, uint16_t data_length) {
    critical_section_enter_blocking(&trace_lock);
    uint8_t slot;
    if (trace_count < XSM3_TRACE_CAPACITY) {
        slot = (uint8_t)((trace_start + trace_count++) % XSM3_TRACE_CAPACITY);
    } else {
        slot = trace_start;
        trace_start = (uint8_t)((trace_start + 1) % XSM3_TRACE_CAPACITY);
    }

    xsm3_trace_entry_t *entry = &trace_entries[slot];
    memset(entry, 0, sizeof(*entry));
    entry->sequence = ++trace_sequence;
    entry->transaction = transaction;
    entry->timestamp_ms = to_ms_since_boot(get_absolute_time());
    entry->event = event;
    entry->status = status;
    entry->status_code = status_code;
    entry->bm_request_type = request->bmRequestType;
    entry->request = request->bRequest;
    entry->value = request->wValue;
    entry->index = request->wIndex;
    entry->requested_length = request->wLength;
    if (data_length > XSM3_TRACE_DATA_MAX) data_length = XSM3_TRACE_DATA_MAX;
    entry->data_length = (uint8_t)data_length;
    if (data_length > 0 && data != NULL) memcpy(entry->data, data, data_length);
    critical_section_exit(&trace_lock);
}

// XOR checksum over the packet body, matching libxsm3's own (unexported)
// xsm3_calculate_checksum() so a locally-patched identification packet
// still passes its internal checksum verification.
static uint8_t identification_checksum(const uint8_t *packet) {
    uint8_t length = (uint8_t)(packet[4] + 5);
    uint8_t sum = 0;
    for (uint8_t i = 5; i < length; i++) sum ^= packet[i];
    return sum;
}

// Handles the 0x81 identification request: pick a fresh random serial and
// identify as the genuine portal the first time; every re-auth after that
// reuses the same identity, since the console stops trusting a device that
// changes serials mid-session.
static void xsm3_handle_identify(void) {
    if (!xsm3_initialised) {
        memcpy(identification_data, xsm3_id_data_ms_controller, sizeof(identification_data));
        for (size_t i = 0; i < 0x0C; i++) {
            identification_data[5 + i] = (uint8_t)(get_rand_32() & 0xFF);
        }
        uint16_t vid = XSM3_VID;
        uint16_t pid = XSM3_PID;
        identification_data[5 + 0xE] = XSM3_CATEGORY;
        memcpy(identification_data + 5 + 0xF, &vid, sizeof(vid));
        memcpy(identification_data + 5 + 0x11, &pid, sizeof(pid));
        identification_data[0x1C] = identification_checksum(identification_data);

        xsm3_initialise_state();
        xsm3_set_identification_data(identification_data);
        xsm3_initialised = true;
    }
}

void xsm3_relay_init(void) {
    memset(&relay_status, 0, sizeof(relay_status));
    relay_status.sidecar_connected = true; // local crypto engine, always ready
    trace_start = 0;
    trace_count = 0;
    trace_sequence = 0;
    trace_transaction = 0;
    xsm3_initialised = false;
    xsm3_response_length = 0;
    xsm3_poll_count = 0;
    critical_section_init(&trace_lock);
    xsm3_debug_log_init();
    printf("XSM3: local authentication engine ready (no real portal/sidecar needed).\n");
}

size_t xsm3_relay_get_debug_log(char *out, size_t max_len) {
    return xsm3_debug_log_get(out, max_len);
}

void xsm3_relay_task(void) {
    // Nothing to pump; challenge init/verify complete synchronously inside
    // xsm3_relay_control_xfer() now that authentication runs locally.
}

void xsm3_relay_get_status(xsm3_relay_status_t *status) {
    *status = relay_status;
}

void xsm3_relay_get_trace(xsm3_trace_snapshot_t *trace) {
    critical_section_enter_blocking(&trace_lock);
    trace->count = trace_count;
    for (uint8_t i = 0; i < trace_count; i++) {
        trace->entries[i] = trace_entries[(trace_start + i) % XSM3_TRACE_CAPACITY];
    }
    critical_section_exit(&trace_lock);
}

bool xsm3_relay_control_xfer(uint8_t rhport, uint8_t stage,
    tusb_control_request_t const *request) {
    if (request->bmRequestType_bit.type != TUSB_REQ_TYPE_VENDOR) {
        return false;
    }

    if (request->bmRequestType == 0xc0 && request->bRequest == 0x01 &&
            request->wValue == 0 && request->wIndex == 0 && request->wLength == 4) {
        if (stage != CONTROL_STAGE_SETUP) return true;
        uint32_t transaction = ++trace_transaction;
        trace_event(XSM3_TRACE_CONTROL_IN_REQUEST, XSM3_TRACE_SENT, 0,
            transaction, request, NULL, 0);
        trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_OK, 0,
            transaction, request, xinput_capability_response,
            sizeof(xinput_capability_response));
        return tud_control_xfer(rhport, request, xinput_capability_response,
            sizeof(xinput_capability_response));
    }
    if (request->bmRequestType == 0xc1 && request->bRequest == 0x01 &&
            request->wValue == 0x0100 && request->wIndex == 0 && request->wLength == 20) {
        if (stage != CONTROL_STAGE_SETUP) return true;
        uint32_t transaction = ++trace_transaction;
        trace_event(XSM3_TRACE_CONTROL_IN_REQUEST, XSM3_TRACE_SENT, 0,
            transaction, request, NULL, 0);
        trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_OK, 0,
            transaction, request, xinput_descriptor_response,
            sizeof(xinput_descriptor_response));
        return tud_control_xfer(rhport, request, xinput_descriptor_response,
            sizeof(xinput_descriptor_response));
    }
    // XInput vibration capabilities -- previously unhandled, causing a STALL
    // on this request (falls outside interface 3, so it hit the generic
    // "unsupported" path since bmRequestType 0xC1 doesn't match the 0x41
    // OUT-ack fallback either).
    if (request->bmRequestType == 0xc1 && request->bRequest == 0x01 &&
            request->wValue == 0x0000 && request->wIndex == 0 && request->wLength == 8) {
        if (stage != CONTROL_STAGE_SETUP) return true;
        uint32_t transaction = ++trace_transaction;
        trace_event(XSM3_TRACE_CONTROL_IN_REQUEST, XSM3_TRACE_SENT, 0,
            transaction, request, NULL, 0);
        trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_OK, 0,
            transaction, request, xinput_vibration_response,
            sizeof(xinput_vibration_response));
        return tud_control_xfer(rhport, request, xinput_vibration_response,
            sizeof(xinput_vibration_response));
    }

    if ((request->wIndex & 0xff) != XSM3_INTERFACE) {
        // Generic ack for vendor OUT requests to any other interface (e.g.
        // controller LED/rumble config on interface 1) that we don't
        // otherwise implement -- a real console sends these, and stalling
        // them (as we did before) risks it giving up on the whole device.
        if (request->bmRequestType == 0x41) {
            if (stage == CONTROL_STAGE_SETUP) {
                return request->wLength == 0 ? tud_control_status(rhport, request)
                                              : tud_control_xfer(rhport, request, control_out_buffer,
                                                    request->wLength > sizeof(control_out_buffer) ?
                                                        sizeof(control_out_buffer) : request->wLength);
            }
            return true;
        }
        if (stage == CONTROL_STAGE_SETUP) {
            relay_status.unsupported_requests++;
            relay_status.last_unsupported_bm_request_type = request->bmRequestType;
            relay_status.last_unsupported_request = request->bRequest;
            relay_status.last_unsupported_value = request->wValue;
            relay_status.last_unsupported_index = request->wIndex;
            relay_status.last_unsupported_length = request->wLength;
        }
        return false;
    }

    bool device_to_host = request->bmRequestType_bit.direction == TUSB_DIR_IN;

    if (stage == CONTROL_STAGE_SETUP) {
        relay_status.requests++;
        relay_status.last_request = request->bRequest;
        relay_status.last_interface = (uint8_t)request->wIndex;
        uint32_t transaction = ++trace_transaction;

        if (device_to_host) {
            trace_event(XSM3_TRACE_CONTROL_IN_REQUEST, XSM3_TRACE_SENT, 0,
                transaction, request, NULL, 0);

            const uint8_t *response = NULL;
            uint16_t response_length = 0;
            switch (request->bRequest) {
                case 0x81:
                    xsm3_handle_identify();
                    response = identification_data;
                    response_length = sizeof(identification_data);
                    break;
                case 0x83:
                    response = xsm3_challenge_response;
                    response_length = xsm3_response_length;
                    break;
                case 0x86:
                    xsm3_poll_count++;
                    response = xsm3_poll_count <= 1 ? STATE_PENDING : STATE_COMPLETE;
                    response_length = sizeof(STATE_COMPLETE);
                    break;
                default:
                    relay_status.errors++;
                    trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_ERROR, -1,
                        transaction, request, NULL, 0);
                    return false;
            }
            if (response_length > request->wLength) response_length = request->wLength;
            trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_OK, 0,
                transaction, request, response, response_length);
            relay_status.responses++;
            return tud_control_xfer(rhport, request, (void *)response, response_length);
        }

        // Host-to-device (OUT).
        if (request->wLength == 0) {
            // 0x84 is a plain "pass" acknowledgement between challenge
            // rounds; nothing for libxsm3 to process.
            trace_event(XSM3_TRACE_CONTROL_OUT_REQUEST, XSM3_TRACE_SENT, 0,
                transaction, request, NULL, 0);
            trace_event(XSM3_TRACE_CONTROL_OUT_ACK, XSM3_TRACE_OK, 0,
                transaction, request, NULL, 0);
            relay_status.responses++;
            return tud_control_status(rhport, request);
        }
        if (request->wLength > sizeof(control_out_buffer)) {
            return false;
        }
        trace_event(XSM3_TRACE_CONTROL_OUT_REQUEST, XSM3_TRACE_SENT, 0,
            transaction, request, NULL, 0);
        return tud_control_xfer(rhport, request, control_out_buffer, request->wLength);
    }

    // Computed at ACK (after the STATUS packet is already on the wire), not
    // DATA, matching EmuToLife's reference -- avoids holding up the status
    // ack behind the crypto computation, in case the console is stricter
    // about control-transfer timing than a permissive USB host would be.
    if (stage == CONTROL_STAGE_ACK && !device_to_host) {
        switch (request->bRequest) {
            case 0x82:
                xsm3_do_challenge_init(control_out_buffer);
                xsm3_response_length = 0x5 + 0x28 + 1;
                xsm3_poll_count = 0;
                memcpy(relay_status.console_id, xsm3_console_id, sizeof(relay_status.console_id));
                break;
            case 0x87:
                xsm3_do_challenge_verify(control_out_buffer);
                xsm3_response_length = 0x5 + 0x10 + 1;
                xsm3_poll_count = 0;
                break;
            default:
                break;
        }
        relay_status.responses++;
        // Logged here (not at SETUP) since control_out_buffer only holds
        // the real received bytes once the DATA stage has landed them.
        trace_event(XSM3_TRACE_CONTROL_OUT_ACK, XSM3_TRACE_OK, 0,
            relay_status.requests, request, control_out_buffer, request->wLength);
        return true;
    }

    return true;
}

bool xsm3_relay_app_exchange(const uint8_t request[32], uint8_t response[32]) {
    (void)request;
    (void)response;
    // No real portal is present anymore; nothing to exchange app-level
    // (LEGO tag) traffic with.
    return false;
}

