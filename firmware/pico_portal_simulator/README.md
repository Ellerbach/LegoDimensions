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

After joining Wi-Fi, open `http://dimensions.local/`. The assigned IPv4 address printed on UART0 is a fallback. UART diagnostics use GPIO0 TX, GPIO1 RX, 115200 baud, and a common ground.

## Xbox 360 sidecar

Xbox 360 requires valid XSM3 accessory authentication. Connect the optional sidecar as documented in [its README](../pico_portal_xsm3_sidecar/README.md). Standard and Xbox One modes do not require the second Pico.

## Firmware behavior

- Wi-Fi and portal settings persist in the last flash sector.
- Changing the portal personality restarts the Pico so USB re-enumerates.
- Leaving the Wi-Fi password blank preserves the current SSID and password.
- The saved password is never returned by the HTTP API.
- If Wi-Fi fails, setup AP mode remains available while USB simulation continues.
