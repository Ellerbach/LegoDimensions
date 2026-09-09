#include "portal_protocol.h"

#include <string.h>

#include "portal_state.h"
#include "xsm3_relay.h"

#define MESSAGE_NORMAL 0x55
#define MESSAGE_EVENT 0x56

static const uint8_t seed_key[16] = {
    0x55, 0xfe, 0xf6, 0x30, 0x62, 0xbf, 0x0b, 0xc1,
    0xc9, 0xb3, 0x7c, 0x34, 0x97, 0x3e, 0x29, 0xfb,
};

static uint32_t rng_state[4];
static bool rng_seeded;

static uint32_t read_u32_le(const uint8_t *data) {
    return (uint32_t)data[0] | ((uint32_t)data[1] << 8) |
        ((uint32_t)data[2] << 16) | ((uint32_t)data[3] << 24);
}

static void write_u32_le(uint8_t *data, uint32_t value) {
    data[0] = (uint8_t)value;
    data[1] = (uint8_t)(value >> 8);
    data[2] = (uint8_t)(value >> 16);
    data[3] = (uint8_t)(value >> 24);
}

static uint32_t rotate_left(uint32_t value, unsigned count) {
    return (value << count) | (value >> (32u - count));
}

static uint32_t portal_rng_next(void) {
    uint32_t temp = rng_state[0] - rotate_left(rng_state[1], 21);
    rng_state[0] = rng_state[1] ^ rotate_left(rng_state[2], 19);
    rng_state[1] = rng_state[2] + rotate_left(rng_state[3], 6);
    rng_state[2] = rng_state[3] + temp;
    rng_state[3] = rng_state[0] + temp;
    return rng_state[3];
}

static void portal_rng_seed(uint32_t seed) {
    rng_state[0] = 0xf1ea5eedu;
    rng_state[1] = seed;
    rng_state[2] = seed;
    rng_state[3] = seed;
    for (int i = 0; i < 42; i++) {
        portal_rng_next();
    }
    rng_seeded = true;
}

static void tea_encrypt(const uint8_t key_bytes[16], const uint8_t input[8], uint8_t output[8]) {
    uint32_t v0 = read_u32_le(input);
    uint32_t v1 = read_u32_le(input + 4);
    uint32_t key[4] = {
        read_u32_le(key_bytes), read_u32_le(key_bytes + 4),
        read_u32_le(key_bytes + 8), read_u32_le(key_bytes + 12),
    };
    uint32_t sum = 0;
    for (int i = 0; i < 32; i++) {
        sum += 0x9e3779b9u;
        v0 += ((v1 << 4) + key[0]) ^ (v1 + sum) ^ ((v1 >> 5) + key[1]);
        v1 += ((v0 << 4) + key[2]) ^ (v0 + sum) ^ ((v0 >> 5) + key[3]);
    }
    write_u32_le(output, v0);
    write_u32_le(output + 4, v1);
}

static void tea_decrypt(const uint8_t key_bytes[16], const uint8_t input[8], uint8_t output[8]) {
    uint32_t v0 = read_u32_le(input);
    uint32_t v1 = read_u32_le(input + 4);
    uint32_t key[4] = {
        read_u32_le(key_bytes), read_u32_le(key_bytes + 4),
        read_u32_le(key_bytes + 8), read_u32_le(key_bytes + 12),
    };
    uint32_t sum = 0xc6ef3720u;
    for (int i = 0; i < 32; i++) {
        v1 -= ((v0 << 4) + key[2]) ^ (v0 + sum) ^ ((v0 >> 5) + key[3]);
        v0 -= ((v1 << 4) + key[0]) ^ (v1 + sum) ^ ((v1 >> 5) + key[1]);
        sum -= 0x9e3779b9u;
    }
    write_u32_le(output, v0);
    write_u32_le(output + 4, v1);
}

static uint8_t checksum(const uint8_t *data, size_t length) {
    uint8_t sum = 0;
    for (size_t i = 0; i < length; i++) {
        sum = (uint8_t)(sum + data[i]);
    }
    return sum;
}

static void make_response(uint8_t id, const uint8_t *payload, uint8_t payload_length, uint8_t output[32]) {
    memset(output, 0, 32);
    output[0] = MESSAGE_NORMAL;
    output[1] = (uint8_t)(1 + payload_length);
    output[2] = id;
    if (payload_length > 0) {
        memcpy(output + 3, payload, payload_length);
    }
    output[3 + payload_length] = checksum(output, 3 + payload_length);
}

static void acknowledge(uint8_t id, uint8_t output[32]) {
    make_response(id, NULL, 0, output);
}

static bool valid_command(const uint8_t frame[32]) {
    uint8_t length = frame[1];
    return frame[0] == MESSAGE_NORMAL && length >= 2 && length <= 29 &&
        frame[length + 2] == checksum(frame, length + 2);
}

void portal_protocol_init(void) {
    rng_seeded = false;
}

bool portal_protocol_handle_frame(const uint8_t frame[32], uint8_t response[32]) {
    if (!valid_command(frame)) {
        return false;
    }

    uint8_t command = frame[2];
    uint8_t id = frame[3];
    const uint8_t *payload = frame + 4;
    uint8_t payload_length = (uint8_t)(frame[1] - 2);

    switch (command) {
        case 0xb0: {
            static const uint8_t wake_data[24] = {
                0x00, 0x10, 0x02, 0x01, 0x02, 0x02, 0x04, 0x06,
                0xf5, 0x00, 0x19, 0x46, 0x53, 0x9f, 0x4d, 0x64,
                0xae, 0x3d, 0x8c, 0x83, 0x07, 0x01, 0x70, 0x1f,
            };
            make_response(id, wake_data, sizeof(wake_data), response);
            return true;
        }
        case 0xb1: {
            if (payload_length != 8) {
                acknowledge(id, response);
                return true;
            }
            uint8_t plain[8];
            uint8_t reply[8];
            tea_decrypt(seed_key, payload, plain);
            portal_rng_seed(read_u32_le(plain));
            uint32_t nonce = read_u32_le(plain + 4);
            write_u32_le(plain, nonce);
            write_u32_le(plain + 4, 0);
            tea_encrypt(seed_key, plain, reply);
            make_response(id, reply, sizeof(reply), response);
            return true;
        }
        case 0xb3: {
            uint8_t plain[8] = {0};
            uint8_t reply[8];
            if (payload_length == 8) {
                tea_decrypt(seed_key, payload, plain);
            }
            uint32_t challenge = read_u32_le(plain);
            write_u32_le(plain, rng_seeded ? portal_rng_next() : 0);
            write_u32_le(plain + 4, challenge);
            tea_encrypt(seed_key, plain, reply);
            make_response(id, reply, sizeof(reply), response);
            return true;
        }
        case 0xc0:
            if (payload_length >= 4) {
                portal_state_set_color(payload[0], payload[1], payload[2], payload[3]);
            }
            acknowledge(id, response);
            return true;
        case 0xc1: {
            uint8_t color[3];
            portal_state_get_color(payload_length ? payload[0] : 0, color);
            make_response(id, color, sizeof(color), response);
            return true;
        }
        case 0xc2:
            if (payload_length >= 6) {
                portal_state_start_fade(payload[0], payload[1], payload[2],
                    payload[3], payload[4], payload[5], false);
            }
            acknowledge(id, response);
            return true;
        case 0xc3:
            if (payload_length >= 7) {
                portal_state_start_flash(payload[0], payload[1], payload[2], payload[3],
                    payload[4], payload[5], payload[6]);
            }
            acknowledge(id, response);
            return true;
        case 0xc4:
            if (payload_length >= 3) {
                portal_state_start_fade(payload[0], payload[1], payload[2], 0, 0, 0, true);
            }
            acknowledge(id, response);
            return true;
        case 0xc5:
            acknowledge(id, response);
            return true;
        case 0xc6:
            if (payload_length >= 18) {
                for (uint8_t pad = 1; pad <= 3; pad++) {
                    const uint8_t *record = &payload[(pad - 1) * 6];
                    if (record[0]) {
                        portal_state_start_fade(pad, record[1], record[2],
                            record[3], record[4], record[5], false);
                    }
                }
            }
            acknowledge(id, response);
            return true;
        case 0xc7:
            if (payload_length >= 21) {
                for (uint8_t pad = 1; pad <= 3; pad++) {
                    const uint8_t *record = &payload[(pad - 1) * 7];
                    if (record[0]) {
                        portal_state_start_flash(pad, record[1], record[2], record[3],
                            record[4], record[5], record[6]);
                    }
                }
            }
            acknowledge(id, response);
            return true;
        case 0xc8:
            if (payload_length >= 12) {
                for (uint8_t pad = 1; pad <= 3; pad++) {
                    const uint8_t *record = &payload[(pad - 1) * 4];
                    portal_state_set_color(pad, record[0] ? record[1] : 0,
                        record[0] ? record[2] : 0, record[0] ? record[3] : 0);
                }
            }
            acknowledge(id, response);
            return true;
        case 0xd0: {
            uint8_t tags[PORTAL_MAX_TAGS * 2];
            size_t count = portal_state_list(tags, PORTAL_MAX_TAGS);
            make_response(id, tags, (uint8_t)(count * 2), response);
            return true;
        }
        case 0xd2: {
            uint8_t result[17] = {1};
            if (payload_length >= 2 && portal_state_read(payload[0], payload[1], result + 1)) {
                result[0] = 0;
            }
            make_response(id, result, sizeof(result), response);
            return true;
        }
        case 0xd3: {
            uint8_t status = 1;
            if (payload_length >= 6 && portal_state_write(payload[0], payload[1], payload + 2)) {
                status = 0;
            }
            make_response(id, &status, 1, response);
            return true;
        }
        case 0xd4: {
            uint8_t result[9] = {1};
            if (payload_length == 8) {
                uint8_t plain[8];
                uint8_t encrypted[8];
                tag_kind_t kind;
                uint16_t model_id;
                tea_decrypt(seed_key, payload, plain);
                uint8_t index = plain[0];
                if (portal_state_get_tag(index, &kind, &model_id)) {
                    if (kind == TAG_CHARACTER) {
                        memset(plain, 0, 4);
                        write_u32_le(plain, model_id);
                        tea_encrypt(seed_key, plain, encrypted);
                        result[0] = 0;
                        memcpy(result + 1, encrypted, sizeof(encrypted));
                    } else {
                        result[0] = 0xf9;
                    }
                } else {
                    result[0] = 0xf2;
                }
            }
            make_response(id, result, sizeof(result), response);
            return true;
        }
        case 0xe1: {
            uint8_t status = 0;
            make_response(id, &status, 1, response);
            return true;
        }
        case 0xe5:
            if (payload_length > 0) {
                portal_state_set_nfc(payload[0] != 0);
            }
            acknowledge(id, response);
            return true;
        default:
            acknowledge(id, response);
            return true;
    }
}

bool portal_protocol_next_event(uint8_t frame[32]) {
    portal_tag_event_t event;
    if (!portal_state_pop_event(&event)) {
        return false;
    }

    memset(frame, 0, 32);
    frame[0] = MESSAGE_EVENT;
    frame[1] = 0x0b;
    frame[2] = event.pad;
    frame[3] = event.type;
    frame[4] = event.index;
    frame[5] = event.removed ? 1 : 0;
    memcpy(frame + 6, event.uid, sizeof(event.uid));
    frame[13] = checksum(frame, 13);
    return true;
}
