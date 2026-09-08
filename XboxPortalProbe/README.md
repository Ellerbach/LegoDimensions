# Xbox portal probe

This diagnostic console targets the Xbox One Lego Dimensions portal at USB ID
`0E6F:0141` and the Xbox 360 Lego Dimensions portal at USB ID `24C6:FA01`. The
main library now supports both portals directly; this application remains
useful for raw protocol investigation and command testing.
The complete transport and initialization sequence is documented in the
[Xbox One portal protocol](../XboxPortalProtocol.md) and the
[Xbox 360 portal protocol](../Xbox360PortalProtocol.md).

## Windows setup

The standard Microsoft driver identifies either portal as an Xbox Gaming Device
and does not expose it to libusb. Use Zadig to replace that device's driver with
WinUSB for testing. Select the device with hardware ID `USB\VID_0E6F&PID_0141`
for the Xbox One portal, or `USB\VID_24C6&PID_FA01` for the Xbox 360 portal.
Changing the driver may prevent the portal from working with an Xbox until the
Microsoft driver is restored.

### Native libusb library

`LibUsbDotNet` 3.0.224 (and its transitive `UsbDotNet.LibUsbNative` dependency,
whose bundled native asset is deliberately excluded via `ExcludeAssets="native"`
in the `.csproj`) does not ship a usable `libusb-1.0.dll` for this project. On
Windows, run:

```powershell
.\XboxPortalProbe\tools\update-libusb.ps1
```

This downloads the latest official libusb Windows release and extracts
`libusb-1.0.dll` into `XboxPortalProbe\native\`, which the `.csproj` then copies
to the build output on every build. That file is not committed to source
control (see `.gitignore`); re-run the script after a fresh clone or whenever
you want to pick up a newer libusb release.

On Linux/macOS, install libusb through your system package manager instead
(e.g. `apt install libusb-1.0-0`, `brew install libusb`); the OS-provided
library is used directly and `native/libusb-1.0.dll` is not applicable there.

Run the probe:

```powershell
dotnet run --project .\XboxPortalProbe\XboxPortalProbe.csproj
```

The probe logs every packet received on endpoint `0x81`. Commands are sent on
endpoint `0x01`:

Before claiming interface 0, the probe attempts to select the portal's first
USB configuration, matching the real-portal setup performed by the MITM host.

```text
gip-init
gip-auth-done
gip-identify
gip 05 00
wake
message C0 01 FF 00 00
test-color-all
test-list-tags
test-read 00 24
send 01-02-FF
quit
```

`gip-init` sends the normal LEGO wake frame wrapped in GIP report `0x21`. If wake
does not receive a prompt response, it sends authentication-complete and waits for
the queued response. An already-authenticated portal responds directly and may not
send ANNOUNCE. IDENTIFY and POWER are not required to operate this portal; use
`gip-identify` separately when investigating its GIP descriptors. The separate
`gip-auth-done` command sends only authentication-complete. Incoming reports are
decoded, and identical consecutive reports are collapsed.

Chunked responses such as `IDENTIFY` are acknowledged and reassembled
automatically. The probe prints the complete payload after the final chunk.

`wake` and `message` wrap normal 32-byte Lego Dimensions frames in GIP report
`0x21`. Report `0x21` is confirmed in both directions: the portal returns its
normal responses and tag events using the same wrapper. `send` writes the
supplied bytes unchanged, which is useful for investigating the Xbox transport.

Type `help` for named tests covering wake/seed/challenge, every color effect,
tag listing and reading, NFC enablement, password mode, model lookup, and tag
writing. Tests with tag-specific or destructive data require explicit payloads;
in particular, `test-write` never supplies default bytes.

## Xbox 360 investigation

If no Xbox One portal is present, the probe falls back to the Xbox 360 portal
at `24C6:FA01`. The main library (`LegoDimensions.LegoPortal`) now supports this
portal directly too; this mode remains useful for raw protocol investigation.
See the [Xbox 360 portal protocol](../Xbox360PortalProtocol.md) for the verified
frame format and concurrency requirements.

The probe claims interface 0 for interrupt endpoints `81/01`. Interface 3
(XSM3 security) is never claimed for normal operation - the toypad replies to
LEGO application commands (wake, colors, tag reads, tag events) without any
authentication. `xsm3-auth` and the `control-in`/`control-out` commands claim
interface 3 on demand purely for XSM3 protocol investigation.

All reads and writes happen sequentially on a single thread. A background
thread continuously reading while a write happens on another thread was found
to silently lose replies on this device/host stack - not a device-side gate.

```text
send <hex>
wake
xinput-led
test-color
test-get-color
test-list-tags
test-read [index page]
test-seed [seed-hex] [nonce-hex]
test-challenge [8-byte-hex]
listen [seconds]
xsm3-auth
control-in <request-type request value index length>
control-out <request-type request value index hex>
quit
```

All numeric fields are hexadecimal. The portal accepts the standard Xbox 360
capability requests and identifies itself through XSM3 request `81`:

```text
control-in c1 81 5b17 0103 1d
494B000017FDB758440233852438032000008082C6240050030001016E
```

The XSM3 record is checksum-valid and advertises security VID/PID `24C6:5000`,
while the outer USB device remains `24C6:FA01`.

`xsm3-auth` performs the complete security exchange: identity (`81`), challenge
initialization (`82`), status polling (`86`), phase-one response (`83`), a fresh
session-specific verify packet (`87`), final response validation (`83`), and
acknowledgement (`84`). The host cryptography is validated against the published
Microsoft-controller transaction. The original LEGO portal uses unpublished
third-party `0x23`/`0x24` key material, so its live phase-one response currently
fails validation. A captured `87` packet cannot be replayed because it is bound
to that authentication session. None of this is required for normal operation.

A real console-to-portal MITM capture confirmed several details of this
implementation and the surrounding sequence: `xsm3-auth`'s two `87`/`86`/`83`
verify rounds (not just one) match a genuine session exactly, byte for byte,
including every request's `wValue`/`wLength`; the wake command's message ID is
simply echoed back rather than required to be `0` (a real console sends `1`);
and the console also issues two vendor requests (`c0 01 0000 0000 0004`,
`c1 01 0100 0000 0014`) and a short, unwrapped 3-byte interrupt report
(`01 03 01`, the `xinput-led` command below) once XSM3 completes and before
the LEGO wake, plus a harmless, repeating, always-stalled vendor control-out
poll of interface 2 (`41 00 001e/001f 0002`) throughout the session - that
interface is the unused chatpad pass-through and is never claimed by this
probe or the main library.

Xbox 360 LEGO messages use report prefix `0B 16` followed by 30 bytes of the
standard LEGO frame. The `wake` command applies this wrapper automatically.
An earlier capture had misread this prefix as `0B 14`; the corrected value is
confirmed by the [dopheideb/LEGODimensions](https://github.com/dopheideb/LEGODimensions)
toy pad firmware extraction.

`test-seed` and `test-challenge` exercise the base LEGO seed/challenge protocol
(commands `0xB1`/`0xB3`), independently of Xbox security. The seed/nonce is
TEA-encrypted with a global key recovered from the same firmware extraction
(`PortalTea.cs`), and `test-challenge` independently reproduces the portal's
internal RNG (`PortalRng.cs`, a Bob Jenkins "burtle"-style generator) to verify
the device's response byte-for-byte. `test-seed` must succeed before
`test-challenge` can predict a matching reply.
