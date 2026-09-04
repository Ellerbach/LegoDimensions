#ifndef UART_BRIDGE_H
#define UART_BRIDGE_H

#include <stdbool.h>
#include <stdint.h>

#include "hardware/uart.h"

#define FRAME_MAX_PAYLOAD 64

#define FRAME_CONTROL_IN_REQUEST 2
#define FRAME_CONTROL_IN_RESPONSE 3
#define FRAME_CONTROL_OUT_REQUEST 4
#define FRAME_CONTROL_OUT_ACK 5
#define FRAME_LINK_STATUS 6
#define FRAME_APP_REQUEST 7
#define FRAME_APP_RESPONSE 8

#define LINK_STATUS_PORTAL_DISCONNECTED 0
#define LINK_STATUS_PORTAL_CONNECTED 1

typedef struct {
    int stage;
    uint8_t kind;
    uint8_t ts_bytes[4];
    uint8_t ts_idx;
    uint8_t length;
    uint8_t payload[FRAME_MAX_PAYLOAD];
    uint8_t payload_idx;
    uint8_t checksum;
    bool checksum_error;
} frame_parser_t;

void frame_parser_init(frame_parser_t *parser);
bool frame_parser_feed(frame_parser_t *parser, uint8_t byte);
void uart_send_frame(uart_inst_t *uart, uint8_t kind,
    uint8_t const *payload, uint8_t length);

#endif
