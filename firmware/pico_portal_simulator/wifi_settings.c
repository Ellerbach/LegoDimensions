#include "wifi_settings.h"

#include <stddef.h>
#include <stdint.h>
#include <string.h>

#include "hardware/flash.h"
#include "hardware/regs/addressmap.h"
#include "pico/flash.h"

#define WIFI_SETTINGS_MAGIC 0x57494649u
#define WIFI_SETTINGS_VERSION 3u
#define WIFI_SETTINGS_OFFSET (PICO_FLASH_SIZE_BYTES - FLASH_SECTOR_SIZE)

typedef struct {
    uint32_t magic;
    uint16_t version;
    uint8_t ssid_length;
    uint8_t password_length;
    char ssid[WIFI_SETTINGS_SSID_MAX];
    char password[WIFI_SETTINGS_PASSWORD_MAX];
    uint32_t checksum;
} stored_wifi_settings_v1_t;

typedef struct {
    uint32_t magic;
    uint16_t version;
    uint8_t ssid_length;
    uint8_t password_length;
    uint8_t portal_variant;
    char ssid[WIFI_SETTINGS_SSID_MAX];
    char password[WIFI_SETTINGS_PASSWORD_MAX];
    uint32_t checksum;
} stored_wifi_settings_v2_t;

typedef struct {
    uint32_t magic;
    uint16_t version;
    uint8_t ssid_length;
    uint8_t password_length;
    uint8_t portal_variant;
    uint8_t state_verbosity;
    char ssid[WIFI_SETTINGS_SSID_MAX];
    char password[WIFI_SETTINGS_PASSWORD_MAX];
    uint32_t checksum;
} stored_wifi_settings_t;

typedef struct {
    uint8_t page[FLASH_PAGE_SIZE];
} flash_write_context_t;

static uint32_t checksum_bytes(const void *settings, size_t length) {
    const uint8_t *data = (const uint8_t *)settings;
    uint32_t value = 2166136261u;
    for (size_t i = 0; i < length; i++) {
        value = (value ^ data[i]) * 16777619u;
    }
    return value;
}

bool wifi_settings_load(wifi_settings_t *settings) {
    memset(settings, 0, sizeof(*settings));
    settings->portal_variant = PORTAL_USB_XBOX_360;
    settings->state_verbosity = STATE_VERBOSITY_NONE;
    const stored_wifi_settings_t *stored =
        (const stored_wifi_settings_t *)(XIP_BASE + WIFI_SETTINGS_OFFSET);
    if (stored->magic != WIFI_SETTINGS_MAGIC) {
        return false;
    }
    if (stored->version == 1) {
        const stored_wifi_settings_v1_t *legacy = (const stored_wifi_settings_v1_t *)stored;
        if (legacy->ssid_length == 0 || legacy->ssid_length > WIFI_SETTINGS_SSID_MAX ||
                legacy->password_length > WIFI_SETTINGS_PASSWORD_MAX ||
                legacy->checksum != checksum_bytes(legacy, offsetof(stored_wifi_settings_v1_t, checksum))) {
            return false;
        }
        memcpy(settings->ssid, legacy->ssid, legacy->ssid_length);
        memcpy(settings->password, legacy->password, legacy->password_length);
        return true;
    }
    if (stored->version == 2) {
        const stored_wifi_settings_v2_t *legacy = (const stored_wifi_settings_v2_t *)stored;
        if (legacy->ssid_length == 0 || legacy->ssid_length > WIFI_SETTINGS_SSID_MAX ||
                legacy->password_length > WIFI_SETTINGS_PASSWORD_MAX ||
                legacy->portal_variant > PORTAL_USB_STANDARD ||
                legacy->checksum != checksum_bytes(legacy, offsetof(stored_wifi_settings_v2_t, checksum))) {
            return false;
        }
        memcpy(settings->ssid, legacy->ssid, legacy->ssid_length);
        memcpy(settings->password, legacy->password, legacy->password_length);
        settings->portal_variant = (portal_usb_variant_t)legacy->portal_variant;
        return true;
    }
    if (stored->version != WIFI_SETTINGS_VERSION || stored->ssid_length == 0 ||
            stored->ssid_length > WIFI_SETTINGS_SSID_MAX ||
            stored->password_length > WIFI_SETTINGS_PASSWORD_MAX ||
            stored->portal_variant > PORTAL_USB_STANDARD ||
            stored->state_verbosity > STATE_VERBOSITY_ALL ||
            stored->checksum != checksum_bytes(stored, offsetof(stored_wifi_settings_t, checksum))) {
        return false;
    }
    memcpy(settings->ssid, stored->ssid, stored->ssid_length);
    memcpy(settings->password, stored->password, stored->password_length);
    settings->portal_variant = (portal_usb_variant_t)stored->portal_variant;
    settings->state_verbosity = (state_verbosity_t)stored->state_verbosity;
    return true;
}

static void write_flash(void *parameter) {
    flash_write_context_t *context = (flash_write_context_t *)parameter;
    flash_range_erase(WIFI_SETTINGS_OFFSET, FLASH_SECTOR_SIZE);
    flash_range_program(WIFI_SETTINGS_OFFSET, context->page, FLASH_PAGE_SIZE);
}

bool wifi_settings_save(const wifi_settings_t *settings) {
    size_t ssid_length = strnlen(settings->ssid, WIFI_SETTINGS_SSID_MAX + 1);
    size_t password_length = strnlen(settings->password, WIFI_SETTINGS_PASSWORD_MAX + 1);
    if (ssid_length == 0 || ssid_length > WIFI_SETTINGS_SSID_MAX ||
        password_length > WIFI_SETTINGS_PASSWORD_MAX ||
        settings->portal_variant > PORTAL_USB_STANDARD ||
        settings->state_verbosity > STATE_VERBOSITY_ALL) {
        return false;
    }

    flash_write_context_t context;
    memset(&context, 0xff, sizeof(context));
    stored_wifi_settings_t *stored = (stored_wifi_settings_t *)context.page;
    stored->magic = WIFI_SETTINGS_MAGIC;
    stored->version = WIFI_SETTINGS_VERSION;
    stored->ssid_length = (uint8_t)ssid_length;
    stored->password_length = (uint8_t)password_length;
    stored->portal_variant = (uint8_t)settings->portal_variant;
    stored->state_verbosity = (uint8_t)settings->state_verbosity;
    memset(stored->ssid, 0, sizeof(stored->ssid));
    memset(stored->password, 0, sizeof(stored->password));
    memcpy(stored->ssid, settings->ssid, ssid_length);
    memcpy(stored->password, settings->password, password_length);
    stored->checksum = checksum_bytes(stored, offsetof(stored_wifi_settings_t, checksum));

    return flash_safe_execute(write_flash, &context, UINT32_MAX) == PICO_OK;
}
