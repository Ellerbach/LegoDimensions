#ifndef USB_TRANSPORT_H
#define USB_TRANSPORT_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    PORTAL_USB_XBOX_360,
    PORTAL_USB_XBOX_ONE,
    PORTAL_USB_STANDARD,
} portal_usb_variant_t;

#define USB_TRACE_CAPACITY 96
#define USB_PACKET_MAX 64

typedef struct {
    uint32_t timestamp_ms;
    bool portal_to_xbox;
    uint8_t length;
    uint8_t data[USB_PACKET_MAX];
} usb_trace_entry_t;

typedef struct {
    bool mounted;
    uint32_t rx_transfers;
    uint32_t tx_transfers;
    uint32_t tx_failures;
    uint32_t xinput_commands;
    uint32_t lego_commands;
    uint32_t wake_commands;
    uint8_t last_rx_length;
    uint8_t last_rx[USB_PACKET_MAX];
    uint8_t last_tx_length;
    uint8_t last_tx[USB_PACKET_MAX];
    uint8_t trace_count;
    usb_trace_entry_t trace[USB_TRACE_CAPACITY];
} usb_transport_status_t;

void usb_transport_init(portal_usb_variant_t variant);
void usb_transport_task(void);
bool usb_transport_ready(void);
void usb_transport_get_status(usb_transport_status_t *status);
portal_usb_variant_t usb_transport_variant(void);
const char *usb_transport_variant_name(portal_usb_variant_t variant);

#ifdef __cplusplus
}
#endif

#endif
