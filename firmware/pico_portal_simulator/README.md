# Pico Portal Simulator

The Pico Portal Simulator turns a Raspberry Pi Pico 2 W into a browser-controlled LEGO Dimensions Toy Pad. It supports three selectable USB personalities:

- Standard PlayStation/Wii U portal (`0E6F:0241`, HID)
- Xbox One/Series portal (`0E6F:0141`, XGIP transport)
- Xbox 360 portal (`24C6:FA01`)

Virtual characters and vehicles expose deterministic UIDs and simulated NFC pages. The browser controls seven physical positions across the center, left, and right pads.

## Build

Follow the common instructions in [firmware/README.md](../README.md). This image must be built for `pico2_w`.

The checked-in `wifi_config.h` intentionally contains empty bootstrap credentials. A fresh device therefore starts the open `Dimension-Toypad-Setup` access point. Connect to it and open `http://192.168.4.1/` to configure a 2.4 GHz Wi-Fi network.

For a private local build, bootstrap credentials may be placed in `wifi_config.h`; never commit real credentials. Runtime settings stored in flash take precedence.

## Access

After joining Wi-Fi, open `http://dimensions.local/`. The assigned IPv4 address is a fallback.

## Optional UART debug output

The simulator has a small diagnostic console that can be monitored with a 3.3 V USB-to-TTL serial adapter. This is useful for checking Wi-Fi connection details, the assigned IP address, mDNS startup, the selected USB personality, and Xbox 360 sidecar status.

| Pico 2 W | USB-to-TTL adapter |
| --- | --- |
| GPIO0 UART0 TX | RX |
| GPIO1 UART0 RX | TX (optional) |
| GND | GND |

Configure the terminal for 115200 baud, 8 data bits, no parity, and 1 stop bit. Only GPIO0 TX, adapter RX, and ground are required to read logs. Use a 3.3 V logic-level adapter; do not connect a 5 V TTL signal to a Pico GPIO. The native USB port must remain dedicated to portal emulation, so this firmware intentionally does not expose a USB serial port.

## Xbox 360 sidecar

Xbox 360 requires valid XSM3 accessory authentication. Connect the optional sidecar as documented in [its README](../pico_portal_xsm3_sidecar/README.md). Standard and Xbox One modes do not require the second Pico.

## Xbox One current limitation

Xbox One uses another way of authenticating. And so far, it's not possible to have it in the simulator and a sidecar has not been implemented yet.

## Firmware behavior

- Wi-Fi and portal settings persist in the last flash sector.
- Changing the portal personality restarts the Pico so USB re-enumerates.
- Leaving the Wi-Fi password blank preserves the current SSID and password.
- The saved password is never returned by the HTTP API.
- If Wi-Fi fails, setup AP mode remains available while USB simulation continues.
