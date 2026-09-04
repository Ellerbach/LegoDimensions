# Xbox 360 XSM3 sidecar

This optional firmware turns a Raspberry Pi Pico 2/RP2350 into a USB host for a genuine Xbox 360 LEGO Dimensions portal. It relays only Xbox Security Method 3 control transfers to the simulator. It is not needed for standard PlayStation/Wii U or Xbox One modes.

## Wiring

| Pico 2 W simulator | Pico 2 sidecar |
| --- | --- |
| GPIO8 UART1 TX | GPIO9 UART1 RX |
| GPIO9 UART1 RX | GPIO8 UART1 TX |
| GND | GND |

The link uses 921600 baud, 8-N-1.

Connect the genuine Xbox 360 portal to the sidecar's native USB data port through a USB host adapter. Supply a stable 5 V VBUS capable of powering the portal. Do not connect independent 5 V sources together, and disconnect the external VBUS arrangement before BOOTSEL flashing from a PC.

## Build

Follow [firmware/README.md](../README.md). Build this image for `pico2`. The output is `build/pico_portal_xsm3_sidecar.uf2`.

The LED remains on while the expected genuine portal (`24C6:FA01`) is mounted. The simulator UART reports sidecar connection and authentication status.
