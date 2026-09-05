#ifndef WIFI_SETTINGS_H
#define WIFI_SETTINGS_H

#include <stdbool.h>

#include "usb_transport.h"

#define WIFI_SETTINGS_SSID_MAX 32
#define WIFI_SETTINGS_PASSWORD_MAX 63

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    STATE_VERBOSITY_NONE,
    STATE_VERBOSITY_XBOX_AUTH,
    STATE_VERBOSITY_TAG_ONLY,
    STATE_VERBOSITY_ALL,
} state_verbosity_t;

typedef struct {
    char ssid[WIFI_SETTINGS_SSID_MAX + 1];
    char password[WIFI_SETTINGS_PASSWORD_MAX + 1];
    portal_usb_variant_t portal_variant;
    state_verbosity_t state_verbosity;
} wifi_settings_t;

bool wifi_settings_load(wifi_settings_t *settings);
bool wifi_settings_save(const wifi_settings_t *settings);

#ifdef __cplusplus
}
#endif

#endif
