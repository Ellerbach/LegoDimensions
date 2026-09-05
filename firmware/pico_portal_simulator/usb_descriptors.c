#include <string.h>

#include "device/usbd_pvt.h"
#include "tusb.h"
#include "usb_descriptors.h"

static portal_usb_variant_t active_variant = PORTAL_USB_XBOX_360;

static tusb_desc_device_t const xbox_360_device_descriptor = {
    .bLength = sizeof(tusb_desc_device_t),
    .bDescriptorType = TUSB_DESC_DEVICE,
    .bcdUSB = 0x0200,
    .bDeviceClass = 0xff,
    .bDeviceSubClass = 0xff,
    .bDeviceProtocol = 0xff,
    .bMaxPacketSize0 = CFG_TUD_ENDPOINT0_SIZE,
    .idVendor = 0x24c6,
    .idProduct = 0xfa01,
    .bcdDevice = 0x0100,
    .iManufacturer = 1,
    .iProduct = 2,
    .iSerialNumber = 3,
    .bNumConfigurations = 1,
};

static tusb_desc_device_t const xbox_one_device_descriptor = {
    .bLength = sizeof(tusb_desc_device_t),
    .bDescriptorType = TUSB_DESC_DEVICE,
    .bcdUSB = 0x0200,
    .bDeviceClass = 0,
    .bDeviceSubClass = 0,
    .bDeviceProtocol = 0,
    .bMaxPacketSize0 = CFG_TUD_ENDPOINT0_SIZE,
    .idVendor = 0x0e6f,
    .idProduct = 0x0141,
    .bcdDevice = 0x0100,
    .iManufacturer = 1,
    .iProduct = 2,
    .iSerialNumber = 3,
    .bNumConfigurations = 1,
};

static tusb_desc_device_t const standard_device_descriptor = {
    .bLength = sizeof(tusb_desc_device_t),
    .bDescriptorType = TUSB_DESC_DEVICE,
    .bcdUSB = 0x0200,
    .bDeviceClass = 0,
    .bDeviceSubClass = 0,
    .bDeviceProtocol = 0,
    .bMaxPacketSize0 = CFG_TUD_ENDPOINT0_SIZE,
    .idVendor = 0x0e6f,
    .idProduct = 0x0241,
    .bcdDevice = 0x0100,
    .iManufacturer = 1,
    .iProduct = 2,
    .iSerialNumber = 3,
    .bNumConfigurations = 1,
};

void usb_descriptors_set_variant(portal_usb_variant_t variant) {
    active_variant = variant;
}

uint8_t const *tud_descriptor_device_cb(void) {
    if (active_variant == PORTAL_USB_XBOX_360) {
        return (uint8_t const *)&xbox_360_device_descriptor;
    }
    if (active_variant == PORTAL_USB_XBOX_ONE) {
        return (uint8_t const *)&xbox_one_device_descriptor;
    }
    return (uint8_t const *)&standard_device_descriptor;
}

#define CONFIG_TOTAL_LEN 153
#define EP_SIZE 32

static uint8_t const xbox_360_configuration_descriptor[] = {
    TUD_CONFIG_DESCRIPTOR(1, 4, 0, CONFIG_TOTAL_LEN, 0x80, 500),

    9, TUSB_DESC_INTERFACE, 0, 0, 2, 0xff, 0x5d, 0x01, 0,
    0x11, 0x21, 0x10, 0x01, 0x21, 0x25, 0x81, 0x14, 0x00,
    0x00, 0x00, 0x00, 0x13, 0x01, 0x08, 0x00, 0x00,
    7, TUSB_DESC_ENDPOINT, 0x01, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 4,
    7, TUSB_DESC_ENDPOINT, 0x81, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 4,

    9, TUSB_DESC_INTERFACE, 1, 0, 4, 0xff, 0x5d, 0x03, 0,
    0x1b, 0x21, 0x00, 0x01, 0x21, 0x01, 0x82, 0x20, 0x01,
    0x02, 0x20, 0x16, 0x83, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x16, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    7, TUSB_DESC_ENDPOINT, 0x82, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 2,
    7, TUSB_DESC_ENDPOINT, 0x02, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 4,
    7, TUSB_DESC_ENDPOINT, 0x83, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 32,
    7, TUSB_DESC_ENDPOINT, 0x03, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 16,

    9, TUSB_DESC_INTERFACE, 2, 0, 1, 0xff, 0x5d, 0x02, 0,
    0x09, 0x21, 0x00, 0x01, 0x21, 0x22, 0x84, 0x07, 0x00,
    7, TUSB_DESC_ENDPOINT, 0x84, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(EP_SIZE), 16,

    9, TUSB_DESC_INTERFACE, 3, 0, 0, 0xff, 0xfd, 0x13, 4,
    0x06, 0x41, 0x00, 0x01, 0x01, 0x03,
};

static uint8_t const xbox_one_configuration_descriptor[] = {
    TUD_CONFIG_DESCRIPTOR(1, 1, 0, 32, 0x80, 500),
    9, TUSB_DESC_INTERFACE, 0, 0, 2, TUSB_CLASS_VENDOR_SPECIFIC, 0x47, 0xd0, 0,
    7, TUSB_DESC_ENDPOINT, 0x01, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(64), 4,
    7, TUSB_DESC_ENDPOINT, 0x81, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(64), 4,
};

static uint8_t const standard_configuration_descriptor[] = {
    TUD_CONFIG_DESCRIPTOR(1, 1, 0, 41, 0x80, 500),
    9, TUSB_DESC_INTERFACE, 0, 0, 2, TUSB_CLASS_HID, 0, 0, 0,
    9, HID_DESC_TYPE_HID, U16_TO_U8S_LE(0x0111), 0, 1,
        HID_DESC_TYPE_REPORT, U16_TO_U8S_LE(29),
    7, TUSB_DESC_ENDPOINT, 0x81, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(32), 1,
    7, TUSB_DESC_ENDPOINT, 0x01, TUSB_XFER_INTERRUPT, U16_TO_U8S_LE(32), 1,
};

static uint8_t const standard_report_descriptor[] = {
    0x06, 0x00, 0xff, 0x09, 0x01, 0xa1, 0x01, 0x19, 0x01, 0x29,
    0x20, 0x15, 0x00, 0x26, 0xff, 0x00, 0x75, 0x08, 0x95, 0x20,
    0x81, 0x00, 0x19, 0x01, 0x29, 0x20, 0x91, 0x00, 0xc0,
};

uint8_t const *tud_descriptor_configuration_cb(uint8_t index) {
    (void)index;
    if (active_variant == PORTAL_USB_XBOX_360) return xbox_360_configuration_descriptor;
    if (active_variant == PORTAL_USB_XBOX_ONE) return xbox_one_configuration_descriptor;
    return standard_configuration_descriptor;
}

uint8_t const *tud_hid_descriptor_report_cb(uint8_t instance) {
    (void)instance;
    return standard_report_descriptor;
}

static const char *xbox_360_strings[] = {
    NULL,
    "Warner Bros.",
    "LEGO(R) DIMENSIONS(TM)",
    "411114ED",
};

static const char *xbox_one_strings[] = {
    NULL,
    "LEGO",
    "Dimensions Portal",
    "00002BECC8D62247",
};

static const char *standard_strings[] = {
    NULL,
    "PDP LIMITED. ",
    "LEGO READER V2.10",
    "P.D.P.000000",
};

#define MS_OS_VENDOR_CODE 0x90
static uint8_t const ms_os_string_desc[] = {
    0x12, TUSB_DESC_STRING,
    'M', 0, 'S', 0, 'F', 0, 'T', 0, '1', 0, '0', 0, '0', 0,
    MS_OS_VENDOR_CODE, 0,
};
static uint8_t const ms_os_compat_id_desc[] = {
    0x28, 0, 0, 0, 0, 1, 4, 0, 1, 0, 0, 0, 0, 0, 0, 0,
    0, 1, 'X', 'G', 'I', 'P', '1', '0', 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
};

static uint16_t const xsm3_string[] = {
    (TUSB_DESC_STRING << 8) | 0xb2,
    0x0058, 0x0062, 0x006f, 0x0078, 0x0020, 0x0053, 0x0065, 0x0063, 0x0075, 0x0072, 0x0069, 0x0074, 0x0079, 0x0020,
    0x004d, 0x0065, 0x0074, 0x0068, 0x006f, 0x0064, 0x0020, 0x0033, 0x002c, 0x0020, 0x0056, 0x0065, 0x0072, 0x0073,
    0x0069, 0x006f, 0x006e, 0x0020, 0x0031, 0x002e, 0x0030, 0x0030, 0x002c, 0x0020, 0x00a9, 0x0020, 0x0032, 0x0030,
    0x0030, 0x0035, 0x0020, 0x004d, 0x0069, 0x0063, 0x0072, 0x006f, 0x0073, 0x006f, 0x0066, 0x0074, 0x0020, 0x0043,
    0x006f, 0x0072, 0x0070, 0x006f, 0x0072, 0x0061, 0x0074, 0x0069, 0x006f, 0x006e, 0x002e, 0x0020, 0x0041, 0x006c,
    0x006c, 0x0020, 0x0072, 0x0069, 0x0067, 0x0068, 0x0074, 0x0073, 0x0020, 0x0072, 0x0065, 0x0073, 0x0065, 0x0072,
    0x0076, 0x0065, 0x0064, 0x002e,
};

static uint16_t string_buffer[32];

uint16_t const *tud_descriptor_string_cb(uint8_t index, uint16_t langid) {
    (void)langid;
    if (active_variant == PORTAL_USB_XBOX_ONE && index == 0xee) {
        return (uint16_t const *)ms_os_string_desc;
    }
    if (active_variant == PORTAL_USB_XBOX_360 && index == 4) {
        return xsm3_string;
    }

    const char *const *strings = active_variant == PORTAL_USB_XBOX_360 ? xbox_360_strings :
        active_variant == PORTAL_USB_XBOX_ONE ? xbox_one_strings : standard_strings;
    size_t length;
    if (index == 0) {
        string_buffer[1] = 0x0409;
        length = 1;
    } else {
        if (index >= 4) {
            return NULL;
        }
        length = strlen(strings[index]);
        if (length > 31) {
            length = 31;
        }
        for (size_t i = 0; i < length; i++) {
            string_buffer[i + 1] = strings[index][i];
        }
    }
    string_buffer[0] = (uint16_t)((TUSB_DESC_STRING << 8) | (length * 2 + 2));
    return string_buffer;
}

bool usb_descriptors_control_xfer(uint8_t rhport, uint8_t stage,
    tusb_control_request_t const *request) {
    if (active_variant != PORTAL_USB_XBOX_ONE || stage != CONTROL_STAGE_SETUP) return false;
    if (request->bmRequestType_bit.type == TUSB_REQ_TYPE_VENDOR &&
            request->bRequest == MS_OS_VENDOR_CODE && request->wIndex == 4) {
        return tud_control_xfer(rhport, request,
            (void *)(uintptr_t)ms_os_compat_id_desc, sizeof(ms_os_compat_id_desc));
    }
    return false;
}

static uint16_t stub_open(uint8_t rhport, tusb_desc_interface_t const *itf, uint16_t max_len) {
    if (active_variant != PORTAL_USB_XBOX_360 ||
            itf->bInterfaceClass != TUSB_CLASS_VENDOR_SPECIFIC || itf->bInterfaceNumber == 0) {
        return 0;
    }
    uint8_t const *descriptor = tu_desc_next(itf);
    uint8_t const *end = (uint8_t const *)itf + max_len;
    for (int guard = 0; guard < 32 && descriptor < end &&
            tu_desc_type(descriptor) != TUSB_DESC_INTERFACE; guard++) {
        if (tu_desc_type(descriptor) == TUSB_DESC_ENDPOINT) {
            usbd_edpt_open(rhport, (tusb_desc_endpoint_t const *)descriptor);
        }
        descriptor = tu_desc_next(descriptor);
    }
    return (uint16_t)(descriptor - (uint8_t const *)itf);
}

static void stub_init(void) {}
static bool stub_deinit(void) { return true; }
static void stub_reset(uint8_t rhport) { (void)rhport; }
static bool stub_control(uint8_t rhport, uint8_t stage, tusb_control_request_t const *request) {
    (void)rhport; (void)stage; (void)request;
    return false;
}
static bool stub_xfer(uint8_t rhport, uint8_t ep, xfer_result_t result, uint32_t bytes) {
    (void)rhport; (void)ep; (void)result; (void)bytes;
    return true;
}

static usbd_class_driver_t const stub_driver = {
    .name = "SIM360",
    .init = stub_init,
    .deinit = stub_deinit,
    .reset = stub_reset,
    .open = stub_open,
    .control_xfer_cb = stub_control,
    .xfer_cb = stub_xfer,
    .sof = NULL,
};

usbd_class_driver_t const *usbd_app_driver_get_cb(uint8_t *count) {
    *count = 1;
    return &stub_driver;
}
