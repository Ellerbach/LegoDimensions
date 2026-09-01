# Xbox 360 Lego Dimensions Portal Protocol

This document describes the protocol used by the Xbox 360 Lego Dimensions portal
with USB ID `24C6:FA01`. It records behavior verified with real hardware and the
[`XboxPortalProbe`](XboxPortalProbe/) diagnostic application.

Unlike the [Xbox One portal](XboxPortalProtocol.md), the Xbox 360 portal does not
use a games-input-protocol transport. It carries the ordinary 32-byte Lego
Dimensions message inside a fixed 2-byte prefix on top of a 32-byte interrupt
report:

```text
USB interrupt transfer
└── 0B 16 prefix
    └── 30 bytes of a 32-byte Lego Dimensions message (last 2 bytes dropped)
```

The embedded message uses the same commands and checksums as non-Xbox portals.
See [Lego Dimensions Communication Protocol](LegoDimensionsProtocol.md) for the
command details and tag data format shared by all portals.

## Status and scope

The following behavior has been verified on an Xbox 360 portal with product ID
`0xFA01`:

- Device discovery and endpoint access through WinUSB/libusb, without claiming
  the XSM3 security interface.
- Wake, tag events, tag reads, pad colors, and tag listing over the wrapped
  32-byte LEGO protocol.
- Reliable continuous background reads while commands are sent from another
  thread (see [Concurrency](#concurrency) below).

The main `LegoPortal` class detects vendor/product ID `24C6:FA01`, performs the
initialization sequence below, and wraps or unwraps the frame automatically.
Applications use the same public API as they use for a standard portal.

## USB transport

| Property | Value |
| --- | --- |
| Vendor ID | `0x24C6` |
| Product ID | `0xFA01` |
| Interface | First interface, normally `0` |
| Host-to-portal endpoint | `0x01` |
| Portal-to-host endpoint | `0x81` |
| Tested Windows driver | WinUSB |

The host should perform these USB steps:

1. Open the device.
2. On Windows, skip resetting/selecting the USB configuration; select it on other
   platforms. This mirrors the reference `toypad.py` implementation, which
   hardcodes the same platform split.
3. Claim the first interface only.
4. Open endpoint `0x81` for reads and endpoint `0x01` for writes.

### No XSM3 binding required

The portal also exposes an XSM3 security interface (interface 3) and identifies
itself through XSM3 request `81` with security VID/PID `24C6:5000`. Unlike an
Xbox 360 game controller, **this portal replies to LEGO application commands
(wake, colors, tag reads, tag events) without ever completing XSM3
authentication.** The main library never claims interface 3.

`XboxPortalProbe` still implements the XSM3 exchange (identity, challenge,
status, phase-one response, verify, acknowledgement) as a diagnostic path,
because the host-side cryptography needed independent verification against the
published Microsoft-controller transaction. That exchange is not part of normal
operation; see the probe's [README](XboxPortalProbe/README.md#xbox-360-investigation)
for details.

## Frame wrapper

Every interrupt report sent or received is 32 bytes:

| b0 | b1 | b2 -> b31 |
| --- | --- | --- |
| `0x0B` | `0x16` | First 30 bytes of the standard 32-byte Lego Dimensions frame |

The prefix bytes are fixed; only the remaining 30 bytes carry the standard
message (checksum, message type, command, payload). Because a standard frame is
32 bytes and only 30 fit after the prefix, its last 2 bytes (always zero padding
on outgoing frames) are not transmitted and are reconstructed as zero on receive.

An earlier capture had misread the second prefix byte as `0x14`; the corrected
`0x16` value is confirmed by the
[dopheideb/LEGODimensions](https://github.com/dopheideb/LEGODimensions) toy pad
firmware extraction.

## Wake

The wake command uses LEGO message ID `0x00` explicitly, rather than the
auto-assigned ID used for every other command. This matches `toypad.py`, which
hardcodes `message_id=0` for wake on this portal. The auto assigned ID should work as well.

## Concurrency

The portal's interrupt endpoints do not tolerate two things a standard or Xbox
One portal tolerates:

- **Concurrent read and write.** A background thread continuously issuing
  `libusb_interrupt_transfer` reads while another thread concurrently issues a
  write on the same device handle silently loses replies. This was confirmed to
  be a host-side (libusb/Windows backend) limitation, not a portal-side gate or
  a LibUsbDotNet-specific defect, by comparing raw sequential calls against a
  hybrid background-read-plus-write shape using direct P/Invoke to
  `libusb-1.0.dll` (see `XboxPortalProbe`'s `raw-wake-360` / `hybrid-wake-360` /
  `hybrid-async-wake-360` commands). The main library enforces full-duration
  mutual exclusion between every read and every write for this portal type.
- **Two in-flight tracked commands.** Sending a second tracked request/reply
  command (e.g. a `Read`) before the first one's reply has arrived appears to
  corrupt or drop one of the two replies, even though each carries a distinct
  message ID. The main library serializes tracked commands for this portal type
  so only one request/reply cycle is outstanding at a time, and skips its
  internal auto-read-on-tag-placement rather than blocking if a command is
  already in flight.

Both constraints are specific to this portal; standard and Xbox One portals are
unaffected.
