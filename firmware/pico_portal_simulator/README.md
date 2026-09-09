# Pico Portal Simulator

Standalone Lego Dimensions portal simulator for a Raspberry Pi Pico 2 W. It exposes the Xbox 360 portal USB shape and serves a browser UI over Wi-Fi for dragging simulated character and vehicle tags onto the center, left, and right pads.

## Current scope

- Xbox 360 USB descriptors (`24C6:FA01`) and `0B 16` LEGO frame wrapper.
- Wake, seed/challenge, color/effect, tag-list, tag-read/write, model, password, and NFC-enable commands.
- Synthetic seven-byte tag UIDs and in-memory NTAG pages.
- Correct UID-derived TEA data for character tags and plain IDs for vehicle tags.
- Queued placement/removal events and up to seven simultaneous tags.
- Live web view of tags and USB-controlled pad colors.
- Transport boundary ready for later standard and Xbox One implementations.

The toy library contains the complete known character list and a starter vehicle
selection from the
[Ellerbach/LegoDimensions wiki](https://github.com/Ellerbach/LegoDimensions/wiki).
Character themes are searchable. Artwork is loaded directly from that wiki's
public attachments, so images require internet access from the browser; tag
simulation itself remains entirely local. The custom ID controls allow any
known character or vehicle ID to be dragged onto a pad.

Click a supported toy before dragging it to cycle through its NFC-backed
forms. Vehicles cycle through their three rebuild IDs; Supergirl cycles to her
Red Lantern character ID. The selected ID is written into the simulated NFC
pages, so USB tag reads report the selected rebuild or form.

## Configure and build

Set `WIFI_SSID` and `WIFI_PASSWORD` in [wifi_config.h](wifi_config.h), then build from the repository root:

```powershell
./scripts/build_in_wsl.ps1 -Board pico2_w -Firmware pico_portal_simulator
```

Flash the generated file:

```powershell
./scripts/flash_pico.ps1 -MassErase -Uf2Path firmware/pico_portal_simulator/build/pico_portal_simulator.uf2
```

The UART console prints the assigned URL after Wi-Fi connects. USB simulation still starts if Wi-Fi cannot connect.

## Change Wi-Fi without rebuilding

Open `/wifi.html` on the portal, for example
`http://192.168.1.72/wifi.html`. Enter the new 2.4 GHz network name and
password. The Pico stores them in the last flash sector, restarts, and uses
the saved settings on subsequent boots. `wifi_config.h` is only the bootstrap
configuration used when no valid saved settings exist.

If the configured network cannot be joined, the Pico starts the open
`Dimension-Toypad-Setup` access point with no password. Connect to it
and open `http://192.168.4.1/` to enter corrected credentials. The setup
network supplies client addresses by DHCP, while USB portal emulation remains
available.

Do not mass-erase the Pico during routine firmware updates if saved Wi-Fi
settings should be retained. A mass erase intentionally clears the settings;
flash the UF2 normally instead.

Network diagnostics are printed at startup and every 15 seconds, including the
SSID, station MAC address, link state, IPv4 address, netmask, gateway, and UI
URL. Debug output uses UART0 at 115200 baud: GPIO0 is TX, GPIO1 is RX, plus a
common ground. The Pico's native USB connection must retain the Xbox portal
descriptors, so it cannot simultaneously appear as a USB serial COM port in
this build. Use a 3.3 V USB-to-UART adapter or a debug probe's serial bridge.

## Testing

For application-protocol testing on a PC, bind interface 0 to WinUSB and use the Xbox 360 mode in `XboxPortalProbe` from `Ellerbach/LegoDimensions`. Open the printed browser URL, drop a toy onto a pad, and use `listen`, `test-list-tags`, and `test-read` to verify events and tag memory.

## Xbox 360 XSM3 authentication

XSM3 accessory authentication runs entirely on-device using
[libxsm3](https://github.com/InvoxiPlayGames/libxsm3) (LGPL-2.1, fetched at
build time via CMake `FetchContent` -- never vendored, so the LGPL source
never lives in this MIT-licensed repo). No second Pico or real portal is
required. Confirmed on real hardware: the console completes the full
`0x81`/`0x82`/`0x86`/`0x83`/`0x84`/`0x87` handshake and the LEGO Dimensions
game accepts the simulator as a genuine portal.

The XSM3 identification packet presents the device as a generic controller
(category `0x02`), not a portal (`0x82`) -- only the "1st party controller"
keyvault root keys are publicly known, so presenting as a portal fails
silently even with an otherwise-correct implementation. The game does not
gate portal recognition on this category byte; it only requires XSM3
authentication to succeed and the LEGO-specific WAKE/protocol handshake on
interface 0 to respond correctly, both of which this firmware does.
