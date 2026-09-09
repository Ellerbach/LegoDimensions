# Xbox One portal capture: human-readable decode

This document decodes a real USB trace between an Xbox One and a LEGO Dimensions portal (`0E6F:0141`). The capture covers USB enumeration, Xbox Game Input Protocol (GIP) identification, authentication, certificate transfer, and attempts to start the LEGO application protocol.

This is a protocol decode, not cryptographic decryption. The GIP framing, offsets, lengths, acknowledgments, text, and DER certificate structure can be interpreted. Authentication challenges, signatures, keys, and certificate bytes remain opaque unless their algorithms and private key material are available.

For the general implementation reference, see [Xbox One Lego Dimensions Portal Protocol](XboxPortalProtocol.md). For the commands transported inside GIP, see [Lego Dimensions Communication Protocol](LegoDimensionsProtocol.md).

## Capture notation

Each JSON line contains:

- `ts`: capture time.
- `direction`: `xbox_to_portal` or `portal_to_xbox`.
- `payload`: one USB interrupt transfer encoded as hexadecimal.
- `sent`: whether the bridge submitted a host-to-portal transfer.
- `fallbackUsed`: whether the bridge used its fallback write path.
- `errno`: operating-system USB error, if any.
- `debug_event`: a USB lifecycle or descriptor callback observed by the bridge firmware.

All multibyte integer values called out below are little-endian unless stated otherwise.

## 1. GIP packet format

Most packets use this header:

| Offset | Size | Field | Meaning |
| ---: | ---: | --- | --- |
| `0` | 1 | Command | GIP command family. |
| `1` | 1 | Options | Client ID in the low nibble; flags in the high nibble. |
| `2` | 1 | Sequence | Per-direction packet or transaction sequence. |
| `3` | LEB128 | Payload length | Number of payload bytes in this USB transfer. |
| next | LEB128, conditional | Chunk value | Present when option `0x80` is set. |
| next | variable | Payload | Command-specific bytes. |

LEB128 stores seven data bits per byte. Bit `0x80` means another length byte follows. Examples from the capture:

- `1C` = 28.
- `3A` = 58.
- `BA 00` = 58.
- `94 01` = 148.
- `B9 06` = 825.

The non-minimal forms such as `BA 00` are valid and occur in genuine portal traffic.

### Options byte

| Mask | Meaning established by this capture and implementation |
| ---: | --- |
| `0x0F` | Client ID. It is zero throughout this trace. |
| `0x10` | Requests an acknowledgment. |
| `0x20` | Control/command context used by this device. Its deeper GIP meaning is not required to parse the capture. |
| `0x40` | First chunk, when combined with `0x80`. |
| `0x80` | A chunk value follows the payload length. |

Common option values in this capture:

| Value | Interpretation |
| ---: | --- |
| `20` | Ordinary control packet. |
| `30` | Ordinary control packet requesting acknowledgment. |
| `A0` | Continuation chunk. |
| `B0` | Continuation chunk requesting acknowledgment. |
| `F0` | First chunk (`80 + 40`) requesting acknowledgment (`10`), with `20` set. |

## 2. High-level timeline

| Time | Direction | Meaning |
| --- | --- | --- |
| `10:26:40.888`–`10:26:41.240` | Portal → Xbox | Repeated GIP `ANNOUNCE` packets while USB enumeration and mounting occur. |
| `10:26:41.272`–`10:26:41.340` | Portal ↔ Xbox | Portal sends a 148-byte, three-chunk `IDENTIFY` record; Xbox acknowledges progress. |
| `10:26:41.368`–`10:26:41.404` | Xbox ↔ Portal | GIP power/status and GIP-level LED setup. |
| `10:26:41.416`–`10:26:41.572` | Xbox ↔ Portal | First authentication request and a 90-byte, two-chunk portal authentication response. |
| `10:26:41.608`–`10:26:42.088` | Xbox ↔ Portal | Second authentication phase; portal sends an 825-byte DER certificate in fifteen chunks. |
| `10:26:42.616` | Xbox → Portal | First wrapped LEGO `B0` wake request. No matching LEGO response is seen. |
| `10:26:43.088` onward | Xbox ↔ Portal | Further authentication state/challenge exchanges and repeated wake attempts. |
| `10:26:44.309` onward | Bridge | Repeated `errno 19` read errors indicate that the USB device is no longer available to the bridge read path. |

The important conclusion is that enumeration and the early GIP handshake work, including a complete certificate transfer, but the trace does not reach an operational LEGO gateway response.

## 3. USB enumeration events

The debug events are not GIP packets. They expose USB device-stack callbacks:

| Code | Name | Meaning |
| ---: | --- | --- |
| `1` | `mount` | The USB host selected a configuration and the device stack mounted. |
| `3` | `device_descriptor_requested` | The Xbox requested the USB device descriptor. Repetition is normal during discovery/reset. |
| `4` | `config_descriptor_requested` | The Xbox requested the configuration descriptor, often first for its header and then for the complete descriptor. |
| `5` | `string_descriptor_requested` | A USB string descriptor was requested; `data` is its index. |
| `7` | `ms_os_compat_requested` | The Microsoft OS compatible-ID descriptor was requested. It identifies the interface as `XGIP10`. |

At `10:26:41.009`–`10:26:41.040`, the Xbox reads the device, configuration, and string descriptors, requests the Microsoft OS descriptor twice, and mounts the interface. A short second descriptor/mount cycle follows at `10:26:41.113`–`10:26:41.196`, consistent with a reset or re-enumeration during driver binding.

## 4. ANNOUNCE (`0x02`)

Repeated packet:

```text
02 20 02 1C
47 22 D6 C8 EC 2B 00 00 6F 0E 41 01
01 00 00 00 07 00 04 00 00 01 01 00 01 00 01 00
```

Header:

| Bytes | Decode |
| --- | --- |
| `02` | GIP `ANNOUNCE`. |
| `20` | Ordinary control options, client 0. |
| `02` | Sequence 2. |
| `1C` | 28-byte payload. |

Payload:

| Offset | Bytes | Interpretation |
| ---: | --- | --- |
| `0` | `47 22 D6 C8 EC 2B` | Device address/identity bytes. |
| `6` | `00 00` | Reserved/unknown. |
| `8` | `6F 0E` | USB vendor ID `0x0E6F`. |
| `10` | `41 01` | USB product ID `0x0141`. |
| `12` | `01 00 00 00 07 00 04 00` | Four 16-bit firmware-version components: `1.0.7.4`. |
| `20` | `00 01 01 00 01 00 01 00` | Four 16-bit hardware-version components: `256.1.1.1`. The first value may use a packed representation; its display meaning is not independently verified. |

The identical ANNOUNCE is transmitted repeatedly about every 16 ms before and just after enumeration. This likely ensures the Xbox observes discovery during resets; it should not be interpreted as multiple portals.

## 5. IDENTIFY (`0x04`)

The portal sends one logical 148-byte record split into three chunks.

### First chunk

```text
04 F0 01 3A 94 01 [58 payload bytes]
```

- `04`: `IDENTIFY`.
- `F0`: first chunk and ACK requested.
- `01`: sequence 1.
- `3A`: 58 bytes carried here.
- `94 01`: total logical size 148 bytes.

The Xbox ACKs it:

```text
01 20 01 09 00 04 20 3A 00 00 00 5A 00
```

The nine-byte ACK payload decodes as:

| Bytes | Meaning |
| --- | --- |
| `00` | Reserved. |
| `04` | Acknowledged command: `IDENTIFY`. |
| `20` | Acknowledged options/client context. |
| `3A 00` | 58 bytes received. |
| `00 00` | Reserved. |
| `5A 00` | 90 bytes remain. |

### Continuation at offset 58

```text
04 A0 01 BA 00 3A [58 payload bytes]
```

- `BA 00`: payload length 58.
- `3A`: destination offset 58.
- The payload contains the readable UTF-16/length-prefixed identity `TTGames.Xbox.Dimensions.Gateway`, followed by GUID-like and capability data.

### Final continuation at offset 116

```text
04 B0 01 A0 00 74 [32 payload bytes]
```

- `A0 00`: payload length 32.
- `74`: destination offset 116.
- `B0` requests a final ACK.

Final ACK:

```text
01 20 01 09 00 04 20 94 00 00 00 00 00
```

`94 00` means all 148 bytes were received and `00 00` means none remain.

### Human interpretation of the IDENTIFY record

The record identifies the application-facing endpoint as:

```text
TTGames.Xbox.Dimensions.Gateway
```

`TTGames` is Traveller's Tales, the game developer. The remaining record includes binary identifiers and capability/property tables. Their container boundaries can be reassembled exactly, but not every proprietary property code has a publicly verified semantic name. They should be retained byte-for-byte by an emulator rather than guessed.

## 6. POWER, status, and GIP LED setup

At `10:26:41.368`:

```text
05 20 02 0F 06 62 45 B8 4B 2D 5A 45 55 00 0F 00 00 00 1F
```

This is GIP command `0x05` (`POWER`) with sequence 2 and a 15-byte command-specific payload. The GUID-like/random-looking bytes and exact subcommand semantics are not proven by this capture alone.

At `10:26:41.384`:

```text
05 20 03 01 00
```

This is another `POWER` packet with one-byte payload `00`.

At `10:26:41.400`:

```text
0A 20 04 03 00 01 14
```

This is GIP command `0x0A` (`LED`) with payload `00 01 14`. It controls a GIP/device LED state, not the three LEGO portal pad colors. Pad colors use embedded LEGO commands `C0`–`C8` after the gateway is active.

The portal responds with command `0x01` ACK packets. For example:

```text
01 20 04 09 02 0A 20 03 00 00 00 03 00
```

This acknowledges command `0A`. Some early ACK counters in this phase appear unusual (`received` and `remaining` both equal the payload length). That behavior is genuine traffic but should not be reinterpreted as ordinary byte reassembly unless a matching chunked transfer exists.

## 7. AUTHENTICATE (`0x06`) overview

GIP command `0x06` carries the Xbox accessory authentication state machine. It is separate from LEGO's own `B1` seed and `B3` challenge commands.

The exchange contains:

1. Xbox authentication requests carrying random-looking challenge/session material.
2. Small portal state responses.
3. A 90-byte portal response split into two chunks.
4. An 825-byte DER X.509 certificate split into fifteen chunks.
5. Additional challenge packets that are retried because the expected next response is absent.

The command can be framed and reassembled, but challenge and signature bodies must be treated as cryptographic opaque data.

## 8. First authentication phase and 90-byte response

Xbox request at `10:26:41.416`:

```text
06 30 01 3A [58-byte payload]
```

- `06`: `AUTHENTICATE`.
- `30`: ordinary control request with ACK requested.
- `01`: sequence 1.
- `3A`: 58-byte payload.
- Payload begins `00 41 00 01 00 2C 01 01 00 28 ...` and contains 40 bytes of session/challenge material plus fixed fields.

The portal acknowledges all 58 bytes:

```text
01 20 01 09 00 06 30 3A 00 00 00 3A 00
```

It then returns a small six-byte authentication payload:

```text
06 30 01 06 00 C1 00 01 00 00
```

The Xbox ACKs it and sends a new 14-byte authentication payload:

```text
06 30 02 0E 00 42 00 02 00 54 00 00 00 00 00 00 00 00
```

The portal's logical 90-byte response follows.

First 58 bytes:

```text
06 F0 02 3A 5A [58 payload bytes]
```

- `F0`: first chunk and ACK requested.
- `5A`: total length 90.

Final 32 bytes:

```text
06 B0 02 20 BA 00 [32 payload bytes]
```

- `BA 00`: continuation offset 58.
- Total after this chunk: 90 bytes.

The Xbox confirms completion:

```text
01 20 02 09 00 06 20 5A 00 00 00 00 00
```

## 9. Certificate transfer: 825 bytes

After another Xbox authentication request, the portal starts this transfer:

```text
06 F0 03 3A B9 06 00 C2 00 03 03 33 03 01 03 2F 30 82 03 2B ...
```

- `06`: `AUTHENTICATE`.
- `F0`: first chunk, ACK requested.
- Sequence `03` remains constant across the logical transfer.
- Payload length is 58.
- `B9 06` is LEB128 825, the total logical length.
- The authentication envelope begins `00 C2 00 03 03 33 03 01 03 2F`.
- `30 82 03 2B` starts a DER `SEQUENCE` whose declared body length is `0x032B` (811 bytes). This is an X.509 certificate nested in the authentication envelope.

### Chunk map

| Packet options | Offset | Data length | End offset | ACK requested |
| --- | ---: | ---: | ---: | --- |
| `F0` first | `0` | 58 | 58 | Yes |
| `A0` | `58` (`3A`) | 58 | 116 | No |
| `A0` | `116` (`74`) | 58 | 174 | No |
| `A0` | `174` (`AE 01`) | 58 | 232 | No |
| `B0` | `232` (`E8 01`) | 53 | 285 | Yes |
| `A0` | `285` (`9D 02`) | 58 | 343 | No |
| `A0` | `343` (`D7 02`) | 58 | 401 | No |
| `A0` | `401` (`91 03`) | 58 | 459 | No |
| `A0` | `459` (`CB 03`) | 58 | 517 | No |
| `B0` | `517` (`85 04`) | 52 | 569 | Yes |
| `A0` | `569` (`B9 04`) | 58 | 627 | No |
| `A0` | `627` (`F3 04`) | 58 | 685 | No |
| `A0` | `685` (`AD 05`) | 58 | 743 | No |
| `A0` | `743` (`E7 05`) | 58 | 801 | No |
| `B0` | `801` (`A1 06`) | 24 | 825 | Yes |

The Xbox sends progress ACKs at 285, 401, 569, 743, and 825 bytes:

```text
... 1D 01 ... 1C 02   # 285 received, 540 remain
... 91 01 ... A8 01   # 401 received, 424 remain
... 39 02 ... 00 01   # 569 received, 256 remain
... E7 02 ... 52 00   # 743 received, 82 remain
... 39 03 ... 00 00   # 825 received, 0 remain
```

Some ACKs are captured after a later portal chunk has already appeared on the wire. They report the Xbox's processed extent, not necessarily every byte that the USB capture thread has already observed. This explains, for example, the 743-byte ACK shortly after the chunk ending at offset 801.

### Readable certificate fields

The reassembled bytes visibly contain these DER distinguished-name values:

- Country: `DE`
- State/province: `Saxony`
- Organization: `Subclass 0001`
- Organizational unit: `Class 03`
- Common name: `Xbox Accessories Class Prod CA 001`
- Validity begins around `2014-10-24 16:04:06Z`
- Validity ends around `2043-02-22 23:59:59Z`
- Public-key algorithm OID: RSA (`1.2.840.113549.1.1.1`)
- Signature algorithm OID: SHA-256 with RSA (`1.2.840.113549.1.1.11`)

The certificate contains a 2048-bit RSA public key. This public certificate is identity material, not the portal's private key. Copying the certificate alone is insufficient to authenticate an emulator; later proof-of-possession steps require responses produced by the associated private key or secure authentication hardware.

## 10. Wrapped LEGO wake command (`0x21`)

At `10:26:42.616`, the Xbox sends:

```text
21 00 01 20
55 0F B0 01 28 63 29 20 4C 45 47 4F 20 32 30 31 34 F7
00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

Outer GIP header:

| Bytes | Meaning |
| --- | --- |
| `21` | `LEGO_GATEWAY`. |
| `00` | No GIP control/chunk flags, client 0. |
| `01` | GIP sequence 1. |
| `20` | Exactly 32 payload bytes. |

Inner LEGO message:

| Offset | Bytes | Meaning |
| ---: | --- | --- |
| `0` | `55` | Normal LEGO message marker. |
| `1` | `0F` | LEGO length: command + message ID + 13 payload bytes. |
| `2` | `B0` | LEGO wake command. |
| `3` | `01` | LEGO message ID 1, independent from the GIP sequence. |
| `4..16` | `28 63 29 20 4C 45 47 4F 20 32 30 31 34` | ASCII `(c) LEGO 2014`. |
| `17` | `F7` | Modulo-256 sum of all preceding LEGO bytes. |
| remainder | zero | Pad to a 32-byte LEGO frame. |

The Xbox retries the same wake with outer GIP sequences `02`, `03`, `04`, `05`, and `06`. No portal-to-Xbox command `0x21` appears in this capture, so the LEGO gateway never returns the wake response during the recorded interval.

This is consistent with authentication not reaching the state where LEGO traffic is accepted.

## 11. Later authentication retries

At `10:26:43.088`:

```text
06 20 04 02 01 01
```

This is an unchunked two-byte authentication state/control message. The exact meaning of state pair `01 01` is not established here.

The Xbox then repeatedly sends a 58-byte, ACK-requested authentication packet beginning:

```text
06 30 05 3A 00 41 00 01 00 2C 01 01 00 28 ...
```

The same packet is retried at roughly 256 ms intervals, indicating that the Xbox has not received the expected portal response or acknowledgment.

Later state/control packets include:

```text
06 20 06 02 01 01
06 20 08 02 01 02
```

The transition from `01 01` to `01 02` is observable, but assigning names such as “success” or “failure” to these values would be speculative without an independent GIP authentication-state reference.

The final packet shown is:

```text
05 20 05 01 07
```

This is a one-byte GIP `POWER` command with value `07`, likely part of teardown or state recovery, but the exact subcommand name is unverified.

## 12. `errno 19` read errors

The capture reports:

```text
{"type":"portal_read_error","errno":19}
```

On Linux/libusb, error 19 is `ENODEV`: “No such device.” In this context it means the bridge's read path no longer has an available USB device or endpoint, often because of reset, re-enumeration, disconnect, or loss of the claimed interface.

It does not decode to a GIP error sent by the portal. It is a host operating-system error. Because it appears repeatedly while host-to-portal writes are still logged, the bridge may be retaining or retrying its write side while its read endpoint/device handle is invalid. Therefore, absence of later portal replies cannot by itself prove that the physical portal intentionally rejected those messages.

## 13. What the capture proves

High confidence:

- The Xbox enumerates the device and asks for `XGIP10` Microsoft compatibility information.
- The portal's ANNOUNCE identifies `0E6F:0141`.
- GIP uses LEB128 payload lengths and chunk offsets/totals.
- IDENTIFY is reassembled to 148 bytes and names `TTGames.Xbox.Dimensions.Gateway`.
- Authentication transfers are chunked and acknowledged at explicit byte boundaries.
- The portal provides an 825-byte authentication record containing an Xbox accessory X.509 certificate.
- The Xbox sends valid GIP-wrapped LEGO `B0` wake requests.
- No wrapped LEGO response is captured.
- The read path subsequently reports operating-system `ENODEV` errors.

Medium confidence:

- GIP `POWER` and `LED` command family names are established, but their payload subfields are only partially understood.
- The repeated `0x0630` messages are authentication challenge/session records; individual opaque fields are not decoded.
- The certificate is part of accessory identity and proof-of-possession setup.

Not established by this trace:

- Private-key material.
- The cryptographic meaning of each challenge byte.
- Semantic names for all IDENTIFY property IDs.
- Exact names for authentication states `01 01` and `01 02`.
- Whether the missing final responses are due to protocol rejection, bridge reset behavior, or physical USB loss.

## 14. Practical emulator implications

A compatible Xbox One emulator needs more than the static USB descriptors and a repeated ANNOUNCE:

1. Correct USB descriptors and Microsoft OS `XGIP10` response.
2. GIP framing, LEB128 encoding, chunk assembly, and ACK generation.
3. A correct IDENTIFY payload and command behavior.
4. Full bidirectional command `0x06` authentication behavior, including certificate and proof-of-possession responses.
5. GIP command `0x21` enabled only at the appropriate authentication state.
6. The normal 32-byte LEGO command/event protocol inside `0x21`.

The certificate bytes may be replayable as public identity data, but challenge responses cannot generally be replayed because they are session-dependent. The trace is sufficient to reproduce packet framing and certificate chunking, but not sufficient on its own to synthesize valid cryptographic authentication.
