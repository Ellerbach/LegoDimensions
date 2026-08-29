# Xbox portal probe

This diagnostic console targets the Xbox One Lego Dimensions portal at USB ID
`0E6F:0141`. The main library now supports this portal directly; this application
remains useful for raw protocol investigation and command testing.
The complete transport and initialization sequence is documented in the
[Xbox One portal protocol](../XboxPortalProtocol.md).

## Windows setup

The standard Microsoft driver identifies the portal as an Xbox Gaming Device and
does not expose it to libusb. Use Zadig to replace that device's driver with
WinUSB for testing. Select the device with hardware ID `USB\VID_0E6F&PID_0141`.
Changing the driver may prevent the portal from working with an Xbox until the
Microsoft driver is restored.

The project references the native libusb runtime package, which places the
platform-specific `libusb-1.0` library in the build output automatically.

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
at `24C6:FA01`. This mode exposes raw endpoint and vendor control transfers:

The probe claims interface 0 for interrupt endpoints `81/01` and interface 3
for XSM3 control transfers. Both interfaces are required on Windows to keep
interrupt monitoring active while issuing security requests through WinUSB.

```text
send <hex>
wake
xinput-led
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
to that authentication session.

Xbox 360 LEGO messages use report prefix `0B 14` followed by 30 bytes of the
standard LEGO frame. The `wake` command applies this wrapper automatically.
On original hardware, wrapped wake receives no reply before XSM3, including
after XInput LED assignment or an early `84` acknowledgement. Interface 0
therefore appears to remain gated until the full security exchange completes.
Authentication and application commands remain diagnostic work and are not
supported by the main library.
