#include "xsm3_relay.h"

#include <stdio.h>
#include <string.h>

#include "device/dcd.h"
#include "hardware/gpio.h"
#include "hardware/uart.h"
#include "pico/critical_section.h"
#include "pico/time.h"

#include "uart_bridge.h"

#define RELAY_UART uart1
#define RELAY_UART_TX_PIN 8
#define RELAY_UART_RX_PIN 9
#define RELAY_UART_BAUD 921600
#define XSM3_INTERFACE 3
#define XSM3_TIMEOUT_MS 2000

static frame_parser_t parser;
static uint8_t control_out_buffer[FRAME_MAX_PAYLOAD];
static uint8_t control_in_buffer[FRAME_MAX_PAYLOAD];
static bool control_in_pending;
static uint8_t control_in_rhport;
static tusb_control_request_t control_in_request;
static absolute_time_t control_in_deadline;
static uint32_t control_in_transaction;
static bool xsm3_ack_84_sent;
static xsm3_relay_status_t relay_status;
static xsm3_trace_entry_t trace_entries[XSM3_TRACE_CAPACITY];
static uint8_t trace_start;
static uint8_t trace_count;
static uint32_t trace_sequence;
static uint32_t trace_transaction;
static critical_section_t trace_lock;
static uint8_t xinput_capability_response[4] = {0x41, 0x11, 0x14, 0xed};
static uint8_t xinput_descriptor_response[20] = {0x00, 0x14};

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

static void stall_control(uint8_t rhport) {
    dcd_edpt_stall(rhport, 0);
    dcd_edpt_stall(rhport, TUSB_DIR_IN_MASK);
}

static int pump_frame(void) {
    while (uart_is_readable(RELAY_UART)) {
        if (!frame_parser_feed(&parser, uart_getc(RELAY_UART))) continue;

        if (parser.kind == FRAME_LINK_STATUS && parser.length >= 1) {
            bool connected = parser.payload[0] == LINK_STATUS_PORTAL_CONNECTED;
            if (connected != relay_status.sidecar_connected) {
                relay_status.sidecar_connected = connected;
                printf("XSM3 sidecar: real portal %s.\n", connected ? "connected" : "disconnected");
            }
        } else if (parser.kind == FRAME_CONTROL_IN_RESPONSE && control_in_pending) {
            control_in_pending = false;
            if (parser.length < 1 || parser.payload[0] != 0) {
                relay_status.errors++;
                trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_ERROR,
                    parser.length >= 1 ? parser.payload[0] : -1,
                    control_in_transaction, &control_in_request,
                    parser.length > 1 ? parser.payload + 1 : NULL,
                    parser.length > 1 ? parser.length - 1 : 0);
                stall_control(control_in_rhport);
            } else {
                relay_status.responses++;
                uint16_t length = parser.length - 1;
                if (length > control_in_request.wLength) length = control_in_request.wLength;
                trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_OK, 0,
                    control_in_transaction, &control_in_request,
                    parser.payload + 1, length);
                memcpy(control_in_buffer, parser.payload + 1, length);
                tud_control_xfer(control_in_rhport, &control_in_request,
                    control_in_buffer, length);
            }
        }
        return parser.kind;
    }
    return -1;
}

static bool wait_for_frame(uint8_t wanted_kind) {
    absolute_time_t deadline = make_timeout_time_ms(XSM3_TIMEOUT_MS);
    while (!time_reached(deadline)) {
        if (pump_frame() == wanted_kind) return true;
    }
    return false;
}

static bool relay_control_out(tusb_control_request_t const *request, uint16_t data_length,
    uint32_t transaction) {
    if (data_length + 6 > FRAME_MAX_PAYLOAD) return false;

    trace_event(XSM3_TRACE_CONTROL_OUT_REQUEST, XSM3_TRACE_SENT, 0,
        transaction, request, control_out_buffer, data_length);

    uint8_t payload[FRAME_MAX_PAYLOAD] = {
        request->bmRequestType, request->bRequest,
        (uint8_t)request->wValue, (uint8_t)(request->wValue >> 8),
        (uint8_t)request->wIndex, (uint8_t)(request->wIndex >> 8),
    };
    memcpy(payload + 6, control_out_buffer, data_length);
    uart_send_frame(RELAY_UART, FRAME_CONTROL_OUT_REQUEST,
        payload, (uint8_t)(data_length + 6));

    if (!wait_for_frame(FRAME_CONTROL_OUT_ACK)) {
        relay_status.timeouts++;
        trace_event(XSM3_TRACE_CONTROL_OUT_ACK, XSM3_TRACE_TIMEOUT, -1,
            transaction, request, NULL, 0);
        return false;
    }
    if (parser.length >= 1 && parser.payload[0] == 0) {
        relay_status.responses++;
        trace_event(XSM3_TRACE_CONTROL_OUT_ACK, XSM3_TRACE_OK, 0,
            transaction, request, NULL, 0);
        return true;
    }
    relay_status.errors++;
    trace_event(XSM3_TRACE_CONTROL_OUT_ACK, XSM3_TRACE_ERROR,
        parser.length >= 1 ? parser.payload[0] : -1,
        transaction, request, NULL, 0);
    return false;
}

void xsm3_relay_init(void) {
    uart_init(RELAY_UART, RELAY_UART_BAUD);
    gpio_set_function(RELAY_UART_TX_PIN, GPIO_FUNC_UART);
    gpio_set_function(RELAY_UART_RX_PIN, GPIO_FUNC_UART);
    uart_set_hw_flow(RELAY_UART, false, false);
    uart_set_format(RELAY_UART, 8, 1, UART_PARITY_NONE);
    uart_set_fifo_enabled(RELAY_UART, true);
    frame_parser_init(&parser);
    memset(&relay_status, 0, sizeof(relay_status));
    trace_start = 0;
    trace_count = 0;
    trace_sequence = 0;
    trace_transaction = 0;
    critical_section_init(&trace_lock);
    printf("XSM3 sidecar UART: GPIO8 TX, GPIO9 RX at 921600 baud.\n");
}

void xsm3_relay_task(void) {
    while (pump_frame() >= 0) {}
    if (control_in_pending && time_reached(control_in_deadline)) {
        control_in_pending = false;
        relay_status.timeouts++;
        trace_event(XSM3_TRACE_CONTROL_IN_RESPONSE, XSM3_TRACE_TIMEOUT, -1,
            control_in_transaction, &control_in_request, NULL, 0);
        stall_control(control_in_rhport);
        printf("XSM3 sidecar response timed out.\n");
    }
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

    if ((request->wIndex & 0xff) != XSM3_INTERFACE) {
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
            if (request->bRequest == 0x81) xsm3_ack_84_sent = false;
            uint8_t payload[8] = {
                request->bmRequestType, request->bRequest,
                (uint8_t)request->wValue, (uint8_t)(request->wValue >> 8),
                (uint8_t)request->wIndex, (uint8_t)(request->wIndex >> 8),
                (uint8_t)request->wLength, (uint8_t)(request->wLength >> 8),
            };
            trace_event(XSM3_TRACE_CONTROL_IN_REQUEST, XSM3_TRACE_SENT, 0,
                transaction, request, NULL, 0);
            uart_send_frame(RELAY_UART, FRAME_CONTROL_IN_REQUEST, payload, sizeof(payload));
            control_in_pending = true;
            control_in_rhport = rhport;
            control_in_request = *request;
            control_in_transaction = transaction;
            control_in_deadline = make_timeout_time_ms(XSM3_TIMEOUT_MS);
            return true;
        }

        if (request->wLength > sizeof(control_out_buffer) || request->wLength + 6 > FRAME_MAX_PAYLOAD) {
            return false;
        }
        if (request->wLength == 0) {
            if (request->bRequest == 0x84 && xsm3_ack_84_sent) {
                return tud_control_status(rhport, request);
            }
            if (!relay_control_out(request, 0, transaction)) return false;
            if (request->bRequest == 0x84) xsm3_ack_84_sent = true;
            return tud_control_status(rhport, request);
        }
        return tud_control_xfer(rhport, request, control_out_buffer, request->wLength);
    }

    if (stage == CONTROL_STAGE_DATA && !device_to_host) {
        return relay_control_out(request, request->wLength, relay_status.requests);
    }
    return true;
}

bool xsm3_relay_app_exchange(const uint8_t request[32], uint8_t response[32]) {
    if (!relay_status.sidecar_connected || control_in_pending) return false;
    uart_send_frame(RELAY_UART, FRAME_APP_REQUEST, request, 32);
    if (!wait_for_frame(FRAME_APP_RESPONSE) || parser.length != 33 || parser.payload[0] != 0) {
        return false;
    }
    memcpy(response, parser.payload + 1, 32);
    return true;
}
