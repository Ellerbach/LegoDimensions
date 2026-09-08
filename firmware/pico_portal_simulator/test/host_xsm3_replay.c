// Standalone x86 replay harness: feeds this session's real captured bytes
// through the unmodified libxsm3 source to check whether the ARM-built
// firmware's crypto output matches a desktop build given identical inputs.
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "xsm3.h"
#include "excrypt.h"
#include "usbdsec.h"

static const uint8_t debug_root_key_0x23[0x10] = {0x82, 0x80, 0x78, 0x68, 0x3A, 0x52, 0x3A, 0x98,
    0x10, 0xF4, 0x0C, 0x12, 0x70, 0x66, 0xDC, 0xBA};
static const uint8_t debug_root_key_0x24[0x10] = {0x66, 0x62, 0x1A, 0x78, 0xF8, 0x60, 0x9C, 0x8A,
    0x26, 0x9A, 0x04, 0xAE, 0xD8, 0x5C, 0x1E, 0xC8};

static void hex_to_bytes(const char *hex, uint8_t *out, size_t out_len) {
    for (size_t i = 0; i < out_len; i++) {
        unsigned int byte;
        sscanf(hex + i * 2, "%2x", &byte);
        out[i] = (uint8_t)byte;
    }
}

static void print_hex(const char *label, const uint8_t *data, size_t len) {
    printf("%s: ", label);
    for (size_t i = 0; i < len; i++) printf("%02x", data[i]);
    printf("\n");
}

int main(void) {
    // From this session's /api/state.json trace:
    uint8_t identification_data[0x1D];
    hex_to_bytes("494b000017342b4bc5b229d10ba1a71b43008082c6240050030001013d",
        identification_data, sizeof(identification_data));

    uint8_t challenge_init[0x22];
    hex_to_bytes("094000001c516209cbba594e63dbf85c991e59b8a0e6d3c01eec0ecf69d9369f6a33",
        challenge_init, sizeof(challenge_init));

    uint8_t expected_response[46];
    hex_to_bytes("494c00002820d642fd14120a3cfac41c7f8f106fcb0fa855477bc0f63d03c1d3c5cf96a4fa31a06783095ecf27c3",
        expected_response, sizeof(expected_response));

    xsm3_initialise_state();
    xsm3_set_identification_data(identification_data);
    xsm3_do_challenge_init(challenge_init);

    print_hex("desktop computed response ", xsm3_challenge_response, 46);
    print_hex("device-reported response  ", expected_response, 46);
    print_hex("console id                ", xsm3_console_id, sizeof(xsm3_console_id));

    uint8_t hash[0x14];
    ExCryptSha(xsm3_console_id, 0x8, NULL, 0, NULL, 0, hash, 0x14);
    uint8_t kv_key1[0x10], kv_key2[0x10];
    UsbdSecXSM3AuthenticationCrypt(debug_root_key_0x23, hash, 0x10, kv_key1, 1);
    UsbdSecXSM3AuthenticationCrypt(debug_root_key_0x24, hash + 0x4, 0x10, kv_key2, 1);
    print_hex("desktop kv_key1           ", kv_key1, sizeof(kv_key1));
    print_hex("desktop kv_key2           ", kv_key2, sizeof(kv_key2));

    int match = memcmp(xsm3_challenge_response, expected_response, 46) == 0;
    printf("MATCH (expected NO - response includes fresh randomness): %s\n", match ? "YES" : "NO");
    return 0;
}
