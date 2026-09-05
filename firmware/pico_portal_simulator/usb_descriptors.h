#ifndef USB_DESCRIPTORS_H
#define USB_DESCRIPTORS_H

#include <stdbool.h>
#include <stdint.h>

#include "tusb.h"
#include "usb_transport.h"

#ifdef __cplusplus
extern "C" {
#endif

void usb_descriptors_set_variant(portal_usb_variant_t variant);
bool usb_descriptors_control_xfer(uint8_t rhport, uint8_t stage,
	tusb_control_request_t const *request);

#ifdef __cplusplus
}
#endif

#endif
