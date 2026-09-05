#ifndef XSM3_RELAY_H
#define XSM3_RELAY_H

#include <stdbool.h>
#include <stdint.h>

#include "tusb.h"

#define XSM3_TRACE_CAPACITY 32
#define XSM3_TRACE_DATA_MAX 64

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    bool sidecar_connected;
    uint32_t requests;
    uint32_t responses;
    uint32_t errors;
    uint32_t timeouts;
    uint32_t unsupported_requests;
    uint8_t last_request;
    uint8_t last_interface;
    uint8_t last_unsupported_bm_request_type;
    uint8_t last_unsupported_request;
    uint16_t last_unsupported_value;
    uint16_t last_unsupported_index;
    uint16_t last_unsupported_length;
} xsm3_relay_status_t;

typedef enum {
    XSM3_TRACE_CONTROL_IN_REQUEST,
    XSM3_TRACE_CONTROL_IN_RESPONSE,
    XSM3_TRACE_CONTROL_OUT_REQUEST,
    XSM3_TRACE_CONTROL_OUT_ACK,
} xsm3_trace_event_t;

typedef enum {
    XSM3_TRACE_SENT,
    XSM3_TRACE_OK,
    XSM3_TRACE_ERROR,
    XSM3_TRACE_TIMEOUT,
} xsm3_trace_status_t;

typedef struct {
    uint32_t sequence;
    uint32_t transaction;
    uint32_t timestamp_ms;
    xsm3_trace_event_t event;
    xsm3_trace_status_t status;
    int16_t status_code;
    uint8_t bm_request_type;
    uint8_t request;
    uint16_t value;
    uint16_t index;
    uint16_t requested_length;
    uint8_t data_length;
    uint8_t data[XSM3_TRACE_DATA_MAX];
} xsm3_trace_entry_t;

typedef struct {
    uint8_t count;
    xsm3_trace_entry_t entries[XSM3_TRACE_CAPACITY];
} xsm3_trace_snapshot_t;

void xsm3_relay_init(void);
void xsm3_relay_task(void);
void xsm3_relay_get_status(xsm3_relay_status_t *status);
void xsm3_relay_get_trace(xsm3_trace_snapshot_t *trace);
bool xsm3_relay_app_exchange(const uint8_t request[32], uint8_t response[32]);
bool xsm3_relay_control_xfer(uint8_t rhport, uint8_t stage,
    tusb_control_request_t const *request);

#ifdef __cplusplus
}
#endif

#endif
