#ifndef PORTAL_PROTOCOL_H
#define PORTAL_PROTOCOL_H

#include <stdbool.h>
#include <stdint.h>

void portal_protocol_init(void);
bool portal_protocol_handle_frame(const uint8_t frame[32], uint8_t response[32]);
bool portal_protocol_next_event(uint8_t frame[32]);

#endif
