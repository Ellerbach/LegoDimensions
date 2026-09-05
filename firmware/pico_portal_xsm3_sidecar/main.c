#include <stdbool.h>
#include <stdint.h>
#include <string.h>

#include "hardware/gpio.h"
#include "hardware/uart.h"
#include "tusb.h"
#include "host/usbh_pvt.h"
#include "pico/stdlib.h"

#include "uart_bridge.h"

#define RELAY_UART uart1
#define RELAY_UART_TX_PIN 8
#define RELAY_UART_RX_PIN 9
#define RELAY_UART_BAUD 921600

#define PORTAL_VID 0x24c6
#define PORTAL_PID 0xfa01

static frame_parser_t parser;
static uint8_t portal_address;
static bool control_busy;
static bool control_in_flight;
static tusb_control_request_t control_setup;
static uint8_t control_buffer[FRAME_MAX_PAYLOAD];
static uint8_t app_buffer[32];
static bool app_busy;
static bool app_reading;

static void send_link_status(bool connected) {
    uint8_t status = connected ? LINK_STATUS_PORTAL_CONNECTED : LINK_STATUS_PORTAL_DISCONNECTED;
    uart_send_frame(RELAY_UART, FRAME_LINK_STATUS, &status, 1);
}

static void send_control_error(uint8_t response_kind) {
    uint8_t status = 1;
    uart_send_frame(RELAY_UART, response_kind, &status, 1);
}

static bool is_portal(uint8_t dev_addr) {
    uint16_t vid = 0;
    uint16_t pid = 0;
    return tuh_vid_pid_get(dev_addr, &vid, &pid) && vid == PORTAL_VID && pid == PORTAL_PID;
}

static bool portal_driver_init(void) {
    return true;
}

static bool portal_driver_deinit(void) {
    return true;
}

static bool portal_driver_open(uint8_t rhport, uint8_t dev_addr,
    tusb_desc_interface_t const *interface, uint16_t max_length) {
    (void)rhport;
    if (interface->bInterfaceClass != TUSB_CLASS_VENDOR_SPECIFIC || !is_portal(dev_addr)) {
        return false;
    }

    uint8_t const *descriptor = tu_desc_next(interface);
    uint8_t const *end = (uint8_t const *)interface + max_length;
    for (int guard = 0; guard < 32 && descriptor < end &&
            tu_desc_type(descriptor) != TUSB_DESC_INTERFACE; guard++) {
        if (tu_desc_type(descriptor) == TUSB_DESC_ENDPOINT &&
                !tuh_edpt_open(dev_addr, (tusb_desc_endpoint_t const *)descriptor)) {
            return false;
        }
        if (descriptor[0] == 0) return false;
        descriptor = tu_desc_next(descriptor);
    }
    return true;
}

static bool portal_driver_set_config(uint8_t dev_addr, uint8_t interface_number) {
    if (!is_portal(dev_addr)) return false;
    usbh_driver_set_config_complete(dev_addr, interface_number);
    return true;
}

static bool portal_driver_xfer(uint8_t dev_addr, uint8_t endpoint,
    xfer_result_t result, uint32_t transferred) {
    (void)dev_addr;
    if (!app_busy) return true;
    if (!app_reading && endpoint == 0x01) {
        if (result == XFER_RESULT_SUCCESS && transferred == sizeof(app_buffer)) {
            app_reading = true;
            memset(app_buffer, 0, sizeof(app_buffer));
            if (usbh_edpt_xfer(portal_address, 0x81, app_buffer, sizeof(app_buffer))) return true;
        }
    } else if (app_reading && endpoint == 0x81) {
        uint8_t response[33] = {result == XFER_RESULT_SUCCESS && transferred == sizeof(app_buffer) ? 0 : 1};
        if (response[0] == 0) memcpy(response + 1, app_buffer, sizeof(app_buffer));
        uart_send_frame(RELAY_UART, FRAME_APP_RESPONSE, response, sizeof(response));
        app_busy = false;
        app_reading = false;
        return true;
    }
    send_control_error(FRAME_APP_RESPONSE);
    app_busy = false;
    app_reading = false;
    return true;
}

static void portal_driver_close(uint8_t dev_addr) {
    if (portal_address == dev_addr) {
        portal_address = 0;
        control_busy = false;
        control_in_flight = false;
        send_link_status(false);
    }
}

static usbh_class_driver_t const portal_driver = {
    .name = "XSM3 portal",
    .init = portal_driver_init,
    .deinit = portal_driver_deinit,
    .open = portal_driver_open,
    .set_config = portal_driver_set_config,
    .xfer_cb = portal_driver_xfer,
    .close = portal_driver_close,
};

usbh_class_driver_t const *usbh_app_driver_get_cb(uint8_t *driver_count) {
    *driver_count = 1;
    return &portal_driver;
}

void tuh_mount_cb(uint8_t dev_addr) {
    if (is_portal(dev_addr)) {
        portal_address = dev_addr;
        send_link_status(true);
    }
}

void tuh_umount_cb(uint8_t dev_addr) {
    portal_driver_close(dev_addr);
}

static void control_complete(tuh_xfer_t *transfer) {
    bool success = transfer->result == XFER_RESULT_SUCCESS;
    bool was_control_in = control_in_flight;
    control_busy = false;
    if (was_control_in) {
        uint8_t response[FRAME_MAX_PAYLOAD];
        uint32_t length = success ? transfer->actual_len : 0;
        if (length > FRAME_MAX_PAYLOAD - 1) length = FRAME_MAX_PAYLOAD - 1;
        response[0] = success ? 0 : 1;
        if (length) memcpy(response + 1, control_buffer, length);
        uart_send_frame(RELAY_UART, FRAME_CONTROL_IN_RESPONSE,
            response, (uint8_t)(length + 1));
    } else {
        uint8_t status = success ? 0 : 1;
        uart_send_frame(RELAY_UART, FRAME_CONTROL_OUT_ACK, &status, 1);
    }
}

static bool start_control_transfer(bool device_to_host) {
    if (control_busy || !portal_address || !tuh_mounted(portal_address)) return false;

    tuh_xfer_t transfer = {
        .daddr = portal_address,
        .ep_addr = 0,
        .setup = &control_setup,
        .buffer = control_buffer,
        .complete_cb = control_complete,
        .user_data = 0,
    };
    control_in_flight = device_to_host;
    control_busy = true;
    if (tuh_control_xfer(&transfer)) return true;
    control_busy = false;
    return false;
}

static void handle_control_in(void) {
    if (parser.length != 8) {
        send_control_error(FRAME_CONTROL_IN_RESPONSE);
        return;
    }

    control_setup = (tusb_control_request_t) {
        .bmRequestType = parser.payload[0],
        .bRequest = parser.payload[1],
        .wValue = (uint16_t)(parser.payload[2] | (parser.payload[3] << 8)),
        .wIndex = (uint16_t)(parser.payload[4] | (parser.payload[5] << 8)),
        .wLength = (uint16_t)(parser.payload[6] | (parser.payload[7] << 8)),
    };
    if (control_setup.wLength > sizeof(control_buffer) - 1 || !start_control_transfer(true)) {
        send_control_error(FRAME_CONTROL_IN_RESPONSE);
    }
}

static void handle_control_out(void) {
    if (parser.length < 6) {
        send_control_error(FRAME_CONTROL_OUT_ACK);
        return;
    }

    uint16_t data_length = parser.length - 6;
    control_setup = (tusb_control_request_t) {
        .bmRequestType = parser.payload[0],
        .bRequest = parser.payload[1],
        .wValue = (uint16_t)(parser.payload[2] | (parser.payload[3] << 8)),
        .wIndex = (uint16_t)(parser.payload[4] | (parser.payload[5] << 8)),
        .wLength = data_length,
    };
    if (data_length) memcpy(control_buffer, parser.payload + 6, data_length);
    if (!start_control_transfer(false)) send_control_error(FRAME_CONTROL_OUT_ACK);
}

static void handle_app_request(void) {
    if (parser.length != sizeof(app_buffer) || app_busy || control_busy ||
            !portal_address || !tuh_mounted(portal_address)) {
        send_control_error(FRAME_APP_RESPONSE);
        return;
    }
    memcpy(app_buffer, parser.payload, sizeof(app_buffer));
    app_busy = true;
    app_reading = false;
    if (!usbh_edpt_xfer(portal_address, 0x01, app_buffer, sizeof(app_buffer))) {
        app_busy = false;
        send_control_error(FRAME_APP_RESPONSE);
    }
}

static void uart_task(void) {
    while (uart_is_readable(RELAY_UART)) {
        if (!frame_parser_feed(&parser, uart_getc(RELAY_UART))) continue;
        if (parser.kind == FRAME_CONTROL_IN_REQUEST) {
            handle_control_in();
        } else if (parser.kind == FRAME_CONTROL_OUT_REQUEST) {
            handle_control_out();
        } else if (parser.kind == FRAME_APP_REQUEST) {
            handle_app_request();
        }
    }
}

int main(void) {
#ifdef PICO_DEFAULT_LED_PIN
    gpio_init(PICO_DEFAULT_LED_PIN);
    gpio_set_dir(PICO_DEFAULT_LED_PIN, GPIO_OUT);
#endif

    uart_init(RELAY_UART, RELAY_UART_BAUD);
    gpio_set_function(RELAY_UART_TX_PIN, GPIO_FUNC_UART);
    gpio_set_function(RELAY_UART_RX_PIN, GPIO_FUNC_UART);
    uart_set_hw_flow(RELAY_UART, false, false);
    uart_set_format(RELAY_UART, 8, 1, UART_PARITY_NONE);
    uart_set_fifo_enabled(RELAY_UART, true);
    frame_parser_init(&parser);

    send_link_status(false);
    tuh_init(0);

    absolute_time_t next_status = make_timeout_time_ms(1000);
    while (true) {
        tuh_task_ext(0, false);
        uart_task();
        if (time_reached(next_status)) {
            send_link_status(portal_address != 0 && tuh_mounted(portal_address));
#ifdef PICO_DEFAULT_LED_PIN
            gpio_put(PICO_DEFAULT_LED_PIN, portal_address != 0);
#endif
            next_status = make_timeout_time_ms(1000);
        }
        tight_loop_contents();
    }
}
