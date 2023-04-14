# Lego Dimensions Communication Protocol

This document aims to explain in details the communication protocol between the portal and the machine.

The portal or Lego Dimensions Starter Kit that are supported are the non XBox ones. The Xbox are currently not documented.

The vendor ID `0x0E6F` is and the product ID `0x0241`. Any other like the Xbox one (0x0141) won't work with this protocol.

## Generalities

All messages sent and receive are always 32 bytes. They are always padded with 0 to complete the side.

The pattern for all the messages is the following:

| b0 | b1 | b2 -> n | bn+1 | bn+2 -> b31 |
|---|---|---|---|---|
|Magic number | length of message| message| checksum| 0 padding to 32 bytes|

The checksum is a simple sum of b0->bn.

Examples:

```text
// received from the portal:
55-12-03-00-63-04-00-00-00-00-00-00-00-01-00-00-00-00-00-00-D2-00-00-00-00-00-00-00-00-00-00-00
// sent to the port:
55-0F-B0-01-28-63-29-20-4C-45-47-4F-20-32-30-31-34-F7-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Event received from the portal:
56-0B-03-00-00-01-04-AF-10-BA-80-49-80-2B-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Sending command from machine to portal

The format is always the following:

| b0 | b1 | b2 | b3| b4 -> n | bn+1 | bn+2 -> b31 |
|---|---|---|---|---|---|---|
|Magic number | length of message| command | message ID| payload | checksum| 0 padding to 32 bytes|

example:

```text
55-06-C0-04-03-FF-00-00-21-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

The message ID is used for the portal to answer. Each command is always confirmed.

### Command answer from the portal

In the communication protocol, each command send from the machine to the portal will get an acknowledge answer from the portal.

The format is the following:

| b0 | b1 | b2 -> n | bn+1 | bn+2 -> b31 |
|---|---|---|---|---|
|Magic number | length of message| message ID + rest of the data| checksum| 0 padding to 32 bytes|

If the message length equals 2, it means it's a simple acknowledge to a command that does not need ay further elements.

When the length is strictly greater than 2, it means that it's an answer to a previous message. Such answer are for example on the command Read.

### Pad

Each pad as a number:

* 0 = all the pads at the same time
* 1 = center
* 2 = left
* 3 = right

### Colors

Colors are all 24 bytes with Red, Green and Blue components.

## Receiving events

All events from the portal to the machine starts with 0x56. They are following the pattern:

| b0 | b1 | b2 | b3 | b4 | b5 | b6 -> b12 | b13 | b14 -> b31|
|---|---|---|---|---|---|---|---|---|
|0x56| length (always 0x0B)| Pad number (1, 2 or 3)| type 0x00, 0x08| tag index| presence (0 if present, 1 if removed)| 7 bytes of card UID| checksum| 0 |

Note: the type values observed are 0x00 when the tag is properly setup and 0x08 when it's not properly setup or another type.

Here are couple of examples:

```text
// Add tag on pad left
56-0B-02-00-00-00-04-64-74-FA-00-49-81-03-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Remove tag on pad left
56-0B-02-00-00-01-04-64-74-FA-00-49-81-04-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Add tag on pad center
56-0B-01-00-00-00-04-64-74-FA-00-49-81-02-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Remove tag on pad central
56-0B-01-00-00-01-04-64-74-FA-00-49-81-03-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Add tag on pad right
56-0B-03-00-00-00-04-64-74-FA-00-49-81-04-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Remove tag on pad right
56-0B-03-00-00-01-04-64-74-FA-00-49-81-05-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00

// Here is a series of addition of different tags on each available spot on the pads
// Notice the index increasing and the pad number
// Notice as well the UID of the card being different each time
56-0B-01-00-00-00-04-64-74-FA-00-49-81-02-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-02-00-01-00-04-F2-70-72-A8-48-80-AC-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-02-00-02-00-04-87-53-BA-80-49-80-46-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-02-00-03-00-04-21-6B-82-A3-48-81-E4-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-03-00-04-00-04-AF-10-BA-80-49-80-2E-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-03-00-05-00-04-CA-8A-B2-6D-40-80-A0-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
56-0B-03-00-06-00-04-27-50-4A-50-49-80-48-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

## Adjusting colors

There are multiple commands to adjust the colors including one to get the color state.

### Command Color 0xC0

This command is to send from the machine to the portal and changes the color immediately of one or all pads at the same time:

| b0 | b1 | b2 | b3 | b4 | b5 | b6 | b7 | b8 | b9 -> b31|
|---|---|---|---|---|---|---|---|---|---|
|0x55| length (0x06)| command (0xC0) | message ID | Pad number (0 for all, 1, 2 or 3)| red | green | blue | checksum| 0 |

Example:

```text
// All pads, Red = FF, Green = FF, Blue = 00 => color = Yellow
55-06-C0-05-00-FF-FF-00-1E-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Command GetColor 0xC1

This commands allows to get the displayed color on a specific pad.

You first have to send the the get color request with the pad you are looking for and then you'll get and answer with the pad color:

| b0 | b1 | b2 | b3| b4 -> n | bn+1 | bn+2 -> b31 |
|---|---|---|---|---|---|---|
|0x55 | length (0x03) | command (0xC1) | message ID| Pad number (1, 2 or 3) | checksum| 0|

The response will be:

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 -> b31 |
|---|---|---|---|---|---|---|---|
|0x55 | length (0x04) | message ID| red | green | blue | checksum| 0 |

```text
// You send:
55-03-C1-03-02-1E-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Portal send back: message ID = 3 like in the previous request, red = 0xFF, green = 0xFF, blue =00 => Color = Yellow
55-04-03-FF-FF-00-5A-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Command set different colors to all pads 0xC8

In this case, the command follow the following description:

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 |b9 |b10 |b11 |b12 |b13 |b14 |b15|b16|b17 |b18 -> b31 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x0E) | command (0xC8)|message ID| center on/off | center red | center green | center blue | left on/off | left red | left green | left blue | right on/off | right red | right green | right blue |checksum| 0 |

Note: 0 for off, 1 for on

```text
// All pads on, center white, blue left, red right
55-0E-C8-02-01-FF-FF-FF-01-00-00-FF-01-FF-00-00-2B-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Command flash a pad 0xC3

This command will flash an individual tag for a certain amount of time and at a certain speed with a specific color:

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 |b9 |b10|b11|b12 |b13 -> b31 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x09) | command (0xC3)|message ID| Pad (1, 2 or 3) | tick on | tick off | tick count (0xFF forever) | red | green | blue | checksum| 0 |

Note:

* The smaller the tick on and tick off values, the faster

```text
// Pad right azure flashing 20 times (10 on and 10 off)
55-09-C3-02-03-14-14-14-F0-FF-FF-50-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Command flash all pads 0xC7

This command is similar to the individual pad flashing except that it will have this effect on all pads at the same time. Each pad can have different settings.

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 |b9 |b10| b11| b12 | b13 | b14 |b15 |b16| b17| b18 | b19 | b20 |b21 |b22| b22 |b23|b24|b25|b26 -> b31 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x17) | command (0xC7)|message ID| Center on/off | tick on | tick off | tick count | red | green | blue | Left on/off | tick on | tick off | tick count | red | green | blue | Right on/off | tick on | tick off | tick count | red | green | blue | checksum| 0 |

Notes:

* 0 for off, 1 for on
* forever flashing = tick count = 0xFF
* The smaller the tick on and tick off values, the faster

```text
// Pad center red asymmetric flashing 40 times (20 on and 20 off)
// Pad left green flashing forever times fast (10 on and 10 off)
// Pad right azure flashing 20 times (10 on and 10 off)
55-17-C7-03-01-0A-1E-28-FF-00-00-01-01-01-FF-00-80-00-01-14-14-14-F0-FF-FF-33-00-00-00-00-00-00
```

### Command fade a pad 0xC2

This command fade a pad. It will take the current color as the original color.

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 |b9|b10 |b11|b12 -> b31 |
|---|---|---|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x08) | command (0xC3)|message ID| Pad (1, 2 or 3) | tick time | tick count (0 never) | red | green | blue | checksum| 0 |

Notes:

* the highest tick time is, the slowest
* tick count will start with the old color and move to the new one then move to the new one to the old one. Meaning that odd numbers will finish on the new color.

```text
// Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.
55-08-C2-02-01-32-05-FF-00-00-58-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Command fade all pads 0xC6

Similar to the individual pad fading, this command will fade all the pads at the same time. Each one can have a different setting and different color.

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8 |b9 |b10| b11| b12 | b13 | b14 |b15 |b16| b17| b18 | b19 | b20 |b21 |b22|b23|b24 -> b31 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x14) | command (0xC6)|message ID| Center on/off | tick time | tick count | red | green | blue | Left on/off | tick time | tick count | red | green | blue | Right on/off | tick time | tick count | red | green | blue | checksum| 0 |

Notes:

* 0 for off, 1 for on
* the highest tick time is, the slowest
* tick count will start with the old color and move to the new one then move to the new one to the old one. Meaning that odd numbers will finish on the new color.

```text
// Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.
// Fade left pad relatively quit fast from the displayed color to green, will finish on the old color as even number of count.
// Fade right will not fade as disabled regardless of the values placed in the Fade Pad.
55-14-C6-02-01-32-05-FF-00-00-01-05-32-00-80-00-01-0A-64-FF-FF-00-8D-00-00-00-00-00-00-00-00-00
```

### Command fade pads to a random value 0xC4

This command will fade one or all pads to a random color but with a specific behavior.

| b0 | b1 | b2 | b3| b4| b6 | b7 | b8| b9 -> b31 |
|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x05) | command (0xC4)|message ID| Pad (1, 2 or 3) | tick time | tick count (0 never) | checksum| 0 |

Notes:

* the highest tick time is, the slowest
* tick count will start with the old color and move to the new one then move to the new one to the old one. Meaning that odd numbers will finish on the new color.

```text
// Randomly fade center pad
55-05-C4-02-02-0A-0A-36-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

## NFC interaction

Once an event is coming thru notifying the presence of a tag, it is possible to get more information about it. This is possible with few functions like Read 0xD2, List tags 0xD0 abd Model 0xD4. It is also possible to write back to the tag.

Tags are NFC card with a 7 Unique ID (card UID). Those card are Ultralight with at least 0x2C (44 decimal) blocks (each block is 4 bytes).

It is interesting to notice that the portal manage to read any type of card like traditional Mifare cards. It would be possible to adjust the code to support as well those cards. It does under the hook means that the RFID reader is using traditional Mifare commands even to read the Ultralight ones. This is what explains why the number of returns read bytes (and needed number of bytes to write) is 16 as it's the Mifare default.

This opens opportunities to use the portal for other behaviors and totally outside of the Lego Dimensions world.

### Reading pages in a tag 0xD2

This command will list all the tags present on the pads. The command to send is like this:

| b0 | b1 | b2 | b3| b4|b5|b6| b7 -> b31 |
|---|---|---|---|---|---|---|---|
|0x55 | length (0x04) | command (0xD2)|message ID| tag index | page number | checksum| 0 |

```text
// Reads tag index 2 and page 0x24
55-04-D2-04-02-24-55-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

Note:

* Page 0x24 is the page containing ann the needed data. See further.
* You will get a buffer of 16 bytes which you will be able to use to check if it's a vehicle, a character and convert to find their ID.

You will get as a return:

| b0 | b1 | b2 | b3| b4-> b19|b20| b21 -> b31 |
|---|---|---|---|---|---|---|
|0x55 | length (0x12) | message ID | error (0 if none) | 16 bytes data | checksum| 0 |

As an example, see the following lines:

```text
// Example of returned value from the previous sent message.
55-12-04-00-BC-04-00-00-00-00-00-00-00-01-00-00-00-00-00-00-2C-00-00-00-00-00-00-00-00-00-00-00
```

### Tag structure

To understand how the data are stored and their value, here is a dump of the card of a character:

```text
  Block: 00 - 04 71 67 9A - Read only: True
  Block: 01 - 5A 54 42 80 - Read only: True
  Block: 02 - CC 48 1F 00 - Read only: True
  Block: 03 - E1 10 12 00 - Read only: True
  Block: 04 - 01 03 A0 0C - Read only: True
  Block: 05 - 34 03 13 D1 - Read only: False
  Block: 06 - 01 0F 54 02 - Read only: False
  Block: 07 - 65 6E 39 36 - Read only: False
  Block: 08 - 30 32 36 30 - Read only: False
  Block: 09 - 31 53 32 36 - Read only: False
  Block: 0A - 31 35 FE 00 - Read only: False
  Block: 0B - 00 00 00 00 - Read only: False
  Block: 0C - 00 00 00 00 - Read only: False
  Block: 0D - 00 00 00 00 - Read only: False
  Block: 0E - 00 00 00 00 - Read only: False
  Block: 0F - 00 00 00 00 - Read only: False
  Block: 10 - 00 00 00 00 - Read only: False
  Block: 11 - 00 00 00 00 - Read only: False
  Block: 12 - 00 00 00 00 - Read only: False
  Block: 13 - 00 00 00 00 - Read only: False
  Block: 14 - 00 00 00 00 - Read only: False
  Block: 15 - 00 00 00 00 - Read only: False
  Block: 16 - 00 00 00 00 - Read only: False
  Block: 17 - 00 00 00 00 - Read only: False
  Block: 18 - 00 00 00 00 - Read only: False
  Block: 19 - 00 00 00 00 - Read only: False
  Block: 1A - 39 36 30 32 - Read only: True
  Block: 1B - 36 30 31 53 - Read only: True
  Block: 1C - 32 36 31 35 - Read only: True
  Block: 1D - 00 00 00 00 - Read only: True
  Block: 1E - 00 00 00 00 - Read only: False
  Block: 1F - 00 00 00 00 - Read only: False
  Block: 20 - 00 00 00 00 - Read only: False
  Block: 21 - 00 00 00 00 - Read only: False
  Block: 22 - 00 00 00 00 - Read only: False
  Block: 23 - 00 00 00 00 - Read only: False
  Block: 24 - 89 CE 51 3D - Read only: True
  Block: 25 - 9E E4 E9 0C - Read only: True
  Block: 26 - 00 00 00 00 - Read only: True
  Block: 27 - 00 00 00 00 - Read only: True
  Block: 28 - 60 0C 3F BD - Read only: True
  Block: 29 - 04 00 00 1E - Read only: True
  Block: 2A - C0 05 00 00 - Read only: True
  Block: 2B - 00 00 00 00 - Read only: True
  Block: 2C - 00 00 00 00 - Read only: True
```

And here is a dump of a tag from a vehicle:

```text
  Block: 00 - 04 27 50 FB - Read only: True
  Block: 01 - 4A 50 49 80 - Read only: True
  Block: 02 - D3 48 1F 00 - Read only: True
  Block: 03 - E1 10 12 00 - Read only: True
  Block: 04 - 01 03 A0 0C - Read only: True
  Block: 05 - 34 03 13 D1 - Read only: False
  Block: 06 - 01 0F 54 02 - Read only: False
  Block: 07 - 65 6E 39 36 - Read only: False
  Block: 08 - 39 36 37 36 - Read only: False
  Block: 09 - 36 53 34 36 - Read only: False
  Block: 0A - 31 35 FE 00 - Read only: False
  Block: 0B - 00 00 00 00 - Read only: False
  Block: 0C - 00 00 00 00 - Read only: False
  Block: 0D - 00 00 00 00 - Read only: False
  Block: 0E - 00 00 00 00 - Read only: False
  Block: 0F - 00 00 00 00 - Read only: False
  Block: 10 - 00 00 00 00 - Read only: False
  Block: 11 - 00 00 00 00 - Read only: False
  Block: 12 - 00 00 00 00 - Read only: False
  Block: 13 - 00 00 00 00 - Read only: False
  Block: 14 - 00 00 00 00 - Read only: False
  Block: 15 - 00 00 00 00 - Read only: False
  Block: 16 - 00 00 00 00 - Read only: False
  Block: 17 - 00 00 00 00 - Read only: False
  Block: 18 - 00 00 00 00 - Read only: False
  Block: 19 - 00 00 00 00 - Read only: False
  Block: 1A - 39 36 39 36 - Read only: True
  Block: 1B - 37 36 36 53 - Read only: True
  Block: 1C - 34 36 31 35 - Read only: True
  Block: 1D - 00 00 00 00 - Read only: True
  Block: 1E - 00 00 00 00 - Read only: False
  Block: 1F - 00 00 00 00 - Read only: False
  Block: 20 - 00 00 00 00 - Read only: False
  Block: 21 - 00 00 00 00 - Read only: False
  Block: 22 - 00 00 00 00 - Read only: False
  Block: 23 - 00 00 00 00 - Read only: False
  Block: 24 - 63 04 00 00 - Read only: False
  Block: 25 - 00 00 00 00 - Read only: False
  Block: 26 - 00 01 00 00 - Read only: True
  Block: 27 - 00 00 00 00 - Read only: True
  Block: 28 - 60 08 3F BD - Read only: True
  Block: 29 - 04 00 00 1E - Read only: True
  Block: 2A - C0 05 00 00 - Read only: True
  Block: 2B - 00 00 00 00 - Read only: True
  Block: 2C - 00 00 00 00 - Read only: True
```

From Ultralight specification, we can understand few things:

* The card is NDEF formatted
* The value of the NDEF message is text
* And there is a replication of the text in blocks 1A to 1C
* The data that interest us is stored from page 0x24 to 0x27

The NDEF usage is still unknown. The values would be:

* 3602601S2615
* 9696766S4615

All the vehicles and characters from the initial series, all finish with 15. S is always placed at the sample place.

The Mission Impossible series has a different ending, the vehicles would be 9813202S1716 and 9812995S1716 for Ethan. Ending may be related to the world the chapters (Vorlon would be 15, Mission Impossible 16) they are part of.

You can note as well that the protection for the different elements is different. To read and to write the tag, you will need a password. We will cover this in a further section.

### Vehicle

The vehicle data are the following:

```text
63 04 00 00
00 00 00 00
00 01 00 00
00 00 00 00
```

The 01 on page 0x26 is the vehicle identifier. The vehicle ID is not encoded, in this case 0x63 0x04 = 0x0463 which translate into 1123 as decimal. In this case it's the Ghost Trap from Ghostbusters.

### Character

Characters are more complicated than the vehicles. The data looks like that:

```text
89 CE 51 3D
9E E4 E9 0C
00 00 00 00
00 00 00 00
```

The character ID is actually encrypted with a TEA algorithm. Encrypt 64 bits with a 128 bit key see [Wikipedia](http://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm).

The encryption key is made from the Card UID with a specific algorithm. It's implementation can be found in [the code](./LegoDimensions/LegoTag.cs).

Once the decryption applied, you'll get the same value on the 2 blocks 0x24 and 0x25. The Id starts at 1.

### Accessing the card

The portal has an embedded way to read and write the card and manage the passwords. If you are using an external reader, check out the implementation in [the code](./LegoDimensions/LegoTag.cs).

### Formatting the card

It is possible to format the card using the keys and possible as well to write new information on tags like making upgrades to any vehicle. This can be achieved with the portal or any external reader.

### List tags 0xD0

This command will list the tags present on the portal. Each tag will have 3 different information:

* it's pad (1 center, 2 left, 3 right)
* it's index (from 0 to 6)
* if the tag is normal or not initialized/with error

You will have to send this simple command

| b0 | b1 | b2 | b3| b4| b5 -> b31 |
|---|---|---|---|---|---|
|0x55 | length (0x05) | command (0xD0)| message ID |checksum| 0 |

And here is the command sent:

```text
55-02-D0-05-2C-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

The answer will be like this:

```text
// Only 1 tag present on the right and no error
55-03-05-30-00-8D-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// All 7 tags present, no error
55-0F-13-30-00-31-00-32-00-23-00-14-00-25-00-26-00-8C-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// All 7 tags present, first tag with error
55-0F-2C-30-08-31-00-32-00-23-00-14-00-25-00-26-00-AD-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

The response will be like:

| b0 | b1 | b2 | b3| b4| bn->bm | bm+1 | bm+2 -> b31 |
|---|---|---|---|---|---|---|---|
|0x55 | length (0x05) | message Id| Pad num (4 bits) and index (4 bits) | Tag type | Repeat up to 6 times |checksum| 0 |

Notes:

* You will get a maximum of 7 tags on the pad
* The pad number (1, 2 or 3) is encoded on the 4 high bits
* The index (0 to 6) is encoded on the 4 low bits
* The tag type is either normal (0) either with error or uninitialized (0x08) values

### Writing a tag 0xD3

This command will write information on a tag on 4 pages.

| b0 | b1 | b2 | b3| b4|b5|b6 -> b9|b10| b11 -> b31 |
|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x09) | command (0xD2)|message ID| tag index | page number | 4 bytes buffer | checksum| 0 |

As an answer, you will get a confirmation if no error.

| b0 | b1 | b2 | b3| b24 -> b31 |
|---|---|---|---|---|
|0x55 | length (0x3) | message ID | error (0 if none) | checksum| 0 |

## Configuration

The portal needs to be awake first, there is a magic command to send for this. You can as well disable or enable the NFC readers and adjust the password behavior.

### Waking up the portal 0xB0

This is the first thing to do to make sure you are up and running.

| b0 | b1 | b2 | b3| b4|b5| b10 -> b31 |
|---|---|---|---|---|---|---|
|0x55 | length (0x0F) | command (0xB0)|message ID| (c) LEGO 2014 | checksum| 0 |

```text
// Message sent
55-0F-B0-01-28-63-29-20-4C-45-47-4F-20-32-30-31-34-F7-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Answer from different the portals:
55-19-01-00-2F-02-01-02-02-04-02-F5-00-19-81-54-D3-A3-44-AE-A5-88-E8-0B-02-60-12-8A-00-00-00-00
55-19-01-00-2F-02-01-02-02-04-02-F5-00-19-86-54-E9-BA-1A-AE-A5-88-E9-15-FF-A0-0A-D2-00-00-00-00
55-19-01-00-2F-02-01-02-02-04-02-F5-00-19-8E-54-6F-87-56-AE-4D-10-25-0D-01-00-01-26-00-00-00-00
55-19-01-00-2F-02-01-02-02-04-02-F5-00-19-81-54-E5-A7-D7-AE-A5-A4-25-04-00-30-3C-7D-00-00-00-00
```

The answer from the portal contains configuration information and most likely serial numbers.

So we can consider that this part is the serial number as it seems changing:

```text
19-81-54-D3-A3-44-AE-A5-88-E8-0B-02-60-12
19-86-54-E9-BA-1A-AE-A5-88-E9-15-FF-A0-0A
19-8E-54-6F-87-56-AE-4D-10-25-0D-01-00-01
19-81-54-E5-A7-D7-AE-A5-A4-25-04-00-30-3C
```

### Seeding command 0xB1

TODO

### Challenge command 0xB3

You can ask for a challenge number to seed.

| b0 | b1 | b2 | b3| b4| b5 | b6| b7 | b8 -> b31 |
|---|---|---|---|---|---|---|---|---|
|0x55 | length (0x02) | command (0xB3)| message ID |checksum| 0 |

```text
// Challenge command sent
55-02-B3-03-0D-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Response
55-09-03-55-0E-B8-F6-64-71-FC-5D-A0-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

Answer will be like this:

| b0 | b1 | b2 | b3-> b10|b11 | b12 -> b31 |
|---|---|---|---|---|---|
|0x55 | length (0x09) | message ID | 8 bytes data | checksum| 0 |

Each time you'll ask for a challenge, you'll get a different one.

### NFC password behavior 0xE1

The default password behavior to access the card is automatic.

The command look like this:

| b0 | b1 | b2 | b3| b4|b5 -> b8| b9 | b10 -> b31 |
|---|---|---|---|---|---|---|---|
|0x55 | length (0x05) | command (0xE1)| index |password mode| 4 bytes custom password |checksum| 0 |

The index behavior is not really known. Disabling the password always seems to work but re-enabling it returns errors except for some values of index.

3 modes exist:

* No password
* Automatic (default)
* Custom password

```text
// Disable password on first index
55-08-E1-03-00-00-00-00-00-00-41-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
// Confirmation without error received
55-04-03-00-AA-55-5B-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

When you unplug and plug the portal, the automatic mode is setup again.

### NFC reader enable command 0xE5

It's possible to deactivate the nfc reader. Default behavior is activated.

| b0 | b1 | b2 | b3| b4| b5 | b6 -> b31 |
|---|---|---|---|---|---|---|
|0x55 | length (0x03) | command (0xE5)| message ID | activation |checksum| 0 |

Note: activation = 0 will disable the NFR readers, 1 will enable it.
