// Forensic decrypt: recover the actual random_controller_data used in this
// session's real capture by round-tripping the device's own sent ciphertext
// through the already-verified kv_key1, then independently re-derive the
// MAC+ACR trailer and compare against what the device actually sent.
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "excrypt.h"
#include "usbdsec.h"

static void hx(const char *hex, uint8_t *out, size_t out_len) {
    for (size_t i = 0; i < out_len; i++) {
        unsigned int b;
        sscanf(hex + i * 2, "%2x", &b);
        out[i] = (uint8_t)b;
    }
}

static void ph(const char *label, const uint8_t *data, size_t len) {
    printf("%-28s: ", label);
    for (size_t i = 0; i < len; i++) printf("%02x", data[i]);
    printf("\n");
}

static const uint8_t key_0x1D[0x10] = {0xE3, 0x5B, 0xFB, 0x1C, 0xCD, 0xAD, 0x32, 0x5B,
    0xF7, 0x0E, 0x07, 0xFD, 0x62, 0x3D, 0xA7, 0xC4};

int main(void) {
    uint8_t kv_key1[0x10]; hx("600bb27d55741f87a43775374dc61075", kv_key1, 16);
    uint8_t kv_key2[0x10]; hx("17a47ddf66bd641a89c0b261ec1980d1", kv_key2, 16);
    uint8_t console_id[8]; hx("095fcac973808182", console_id, 8);

    uint8_t challenge_packet[0x22];
    hx("094000001c5990ef4057167cde8a2eb4cafc6555f834fa2dc911428b07518e1e550a", challenge_packet, 0x22);

    uint8_t challenge_response[46];
    hx("494c000028ebbe09ce32b251b3a27e0a2f42a4387877b95639365f9b6dafecaed19c5fa8c63dd08531ba979700e3",
        challenge_response, 46);

    // identification_data[0x20] repacked per xsm3_set_identification_data(),
    // derived from this session's captured 0x81 response bytes.
    uint8_t identification_data[0x20] = {0};
    {
        uint8_t id_resp[29];
        hx("494b000017f59b27f9ce6e3f69c72e7f01008082c62400500300010162", id_resp, 29);
        uint8_t *id_data = id_resp + 5;
        memcpy(identification_data, id_data, 0xF);
        memcpy(identification_data + 0x10, id_data + 0xF, 2);
        memcpy(identification_data + 0x12, id_data + 0x11, 2);
        memcpy(identification_data + 0x14, id_data + 0x13, 1);
        memcpy(identification_data + 0x15, id_data + 0x16, 1);
        memcpy(identification_data + 0x16, id_data + 0x14, 2);
    }

    // Step 1: decrypt incoming challenge packet with static key 0x1D.
    uint8_t decrypted_in[0x18];
    UsbdSecXSM3AuthenticationCrypt(key_0x1D, challenge_packet + 0x5, 0x18, decrypted_in, 0);
    uint8_t random_console_data[0x10];
    memcpy(random_console_data, decrypted_in, 0x10);
    ph("recovered console_id", decrypted_in + 0x10, 8);
    ph("expected console_id ", console_id, 8);

    // Step 2: independently derive random_console_data_enc (key1-encrypted).
    uint8_t random_console_data_enc[0x10];
    UsbdSecXSM3AuthenticationCrypt(kv_key1, random_console_data, 0x10, random_console_data_enc, 1);

    // Step 3: decrypt the device's ACTUAL sent 32-byte ciphertext body.
    uint8_t decrypted_body[0x20];
    UsbdSecXSM3AuthenticationCrypt(random_console_data_enc, challenge_response + 0x5, 0x20, decrypted_body, 0);
    uint8_t recovered_controller_data[0x10];
    memcpy(recovered_controller_data, decrypted_body, 0x10);
    ph("recovered controller_data", recovered_controller_data, 16);
    ph("recovered console_data(2)", decrypted_body + 0x10, 16);
    ph("expected console_data    ", random_console_data, 16);
    int roundtrip_ok = memcmp(decrypted_body + 0x10, random_console_data, 16) == 0;
    printf("ENCRYPTION ROUND-TRIP MATCH: %s\n\n", roundtrip_ok ? "YES" : "NO");

    // Step 4: re-derive the swapped/encrypted console data + MAC + ACR using
    // the recovered controller_data, and compare against what was sent.
    uint8_t swap[0x10];
    memcpy(swap, random_console_data + 0x8, 0x8);
    memcpy(swap + 0x8, random_console_data, 0x8);
    uint8_t swap_enc[0x10];
    UsbdSecXSM3AuthenticationCrypt(kv_key2, swap, 0x10, swap_enc, 1);

    uint8_t response_packet_mac[0x8];
    UsbdSecXSM3AuthenticationMac(swap_enc, NULL, challenge_response + 0x5, 0x20, response_packet_mac);

    uint8_t acr[0x8];
    UsbdSecXSMAuthenticationAcr(console_id, identification_data, response_packet_mac, acr);

    ph("recomputed ACR", acr, 8);
    ph("device-sent ACR", challenge_response + 0x5 + 0x20, 8);
    int acr_ok = memcmp(acr, challenge_response + 0x5 + 0x20, 8) == 0;
    printf("ACR MATCH: %s\n", acr_ok ? "YES" : "NO");
    return 0;
}
