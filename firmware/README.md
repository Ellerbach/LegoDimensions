# Pico portal firmware

This folder contains firmware for a browser-controlled virtual LEGO Dimensions portal.

| Firmware | Board | Purpose |
| --- | --- | --- |
| `pico_portal_simulator` | Raspberry Pi Pico 2 W | Browser-controlled virtual LEGO Dimensions portal for standard, Xbox One, and Xbox 360 USB personalities. |

## Requirements

- Raspberry Pi Pico SDK 2.3.0 or newer, including submodules.
- CMake 3.13 or newer.
- An Arm GNU embedded toolchain supported by the Pico SDK.
- Ninja or Make.

Set `PICO_SDK_PATH` to the SDK checkout. Do not put Wi-Fi credentials in source control.

## Build

Linux, macOS, or WSL:

```bash
export PICO_SDK_PATH="$HOME/pico-sdk"
./firmware/build-firmware.sh
```

Windows PowerShell with a native Pico toolchain:

```powershell
$env:PICO_SDK_PATH = 'C:\pico\pico-sdk'
./firmware/build-firmware.ps1
```

Output:

- `firmware/pico_portal_simulator/build/pico_portal_simulator.uf2`

To build one image manually:

```bash
cmake -S firmware/pico_portal_simulator -B firmware/pico_portal_simulator/build -DPICO_BOARD=pico2_w -DCMAKE_BUILD_TYPE=Release
cmake --build firmware/pico_portal_simulator/build --parallel
```

## Flash

Hold BOOTSEL while connecting the board, then copy the UF2 to the mounted `RPI-RP2` drive. Flash the simulator normally to retain stored settings; a full-chip erase also clears its saved Wi-Fi, portal personality, and diagnostics settings.

See [Portal simulator](pico_portal_simulator/README.md) and the [end-user guide](PORTAL_SIMULATOR_USER_GUIDE.md).
