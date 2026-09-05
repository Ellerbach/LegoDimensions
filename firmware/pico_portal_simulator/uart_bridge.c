#include "uart_bridge.h"

#include "pico/time.h"

enum { STAGE_SYNC, STAGE_KIND, STAGE_TS, STAGE_LEN, STAGE_PAYLOAD, STAGE_CHECKSUM };

void frame_parser_init(frame_parser_t *parser) {
    parser->stage = STAGE_SYNC;
    parser->checksum_error = false;
}

bool frame_parser_feed(frame_parser_t *parser, uint8_t byte) {
    switch (parser->stage) {
        case STAGE_SYNC:
            if (byte == 0xaa) {
                parser->checksum = byte;
                parser->checksum_error = false;
                parser->stage = STAGE_KIND;
            }
            return false;
        case STAGE_KIND:
            parser->kind = byte;
            parser->checksum ^= byte;
            parser->ts_idx = 0;
            parser->stage = STAGE_TS;
            return false;
        case STAGE_TS:
            parser->ts_bytes[parser->ts_idx++] = byte;
            parser->checksum ^= byte;
            if (parser->ts_idx == 4) parser->stage = STAGE_LEN;
            return false;
        case STAGE_LEN:
            parser->length = byte;
            parser->checksum ^= byte;
            parser->payload_idx = 0;
            if (parser->length > FRAME_MAX_PAYLOAD) {
                parser->stage = STAGE_SYNC;
                return false;
            }
            parser->stage = parser->length ? STAGE_PAYLOAD : STAGE_CHECKSUM;
            return false;
        case STAGE_PAYLOAD:
            parser->payload[parser->payload_idx++] = byte;
            parser->checksum ^= byte;
            if (parser->payload_idx == parser->length) parser->stage = STAGE_CHECKSUM;
            return false;
        case STAGE_CHECKSUM:
            parser->stage = STAGE_SYNC;
            parser->checksum_error = byte != parser->checksum;
            return !parser->checksum_error;
        default:
            parser->stage = STAGE_SYNC;
            return false;
    }
}

void uart_send_frame(uart_inst_t *uart, uint8_t kind,
    uint8_t const *payload, uint8_t length) {
    if (length > FRAME_MAX_PAYLOAD) length = FRAME_MAX_PAYLOAD;

    uint8_t checksum = 0xaa;
    uart_putc_raw(uart, 0xaa);
    checksum ^= kind;
    uart_putc_raw(uart, kind);

    uint32_t timestamp = time_us_32();
    for (int i = 0; i < 4; i++) {
        uint8_t byte = (uint8_t)(timestamp >> (8 * i));
        checksum ^= byte;
        uart_putc_raw(uart, byte);
    }

    checksum ^= length;
    uart_putc_raw(uart, length);
    for (uint8_t i = 0; i < length; i++) {
        checksum ^= payload[i];
        uart_putc_raw(uart, payload[i]);
    }
    uart_putc_raw(uart, checksum);
}
