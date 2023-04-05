# Lego Dimensions portal and tag .NET implementation

The aim of this work is to be able to use Lego Dimensions portal and tags outside of their original purpose. Idea is to automate the usage or the portal led, but as well be able to read tags, with or without the portal to automate other external elements from a simple Raspberry PI for example.

You will find a detailed documentation of the Lego Dimensions protocol [here](./LegoDimensionsProtocol.md). I built this documentation as I did not find anything that detailed.

## Lego Dimensions portal

The Lego Dimensions portals compatible with this implementation are all except the XBox ones.

In this document and in the code, the portal refers to the Lego Dimensions where you place the vehicles and characters.

The Lego Tag or just tag refers to the the vehicle or character that you building when playing the game. They are under the hook NFC cards.

### Driver installation

In order to use the portal, you need to setup USB drivers.

For Windows users (7 and more)

* Plug your Lego Dimensions portal.
* Download [Zadig](https://zadig.akeo.ie/) and run it
* Select "LEGO Reader V2.10" in the devices list (if necessary, use the Options>List All Devices menu)
* Select `WinUSB` (libusb will also work) in the list at right of the green arrow and press the install button.

For Linux users

* Install libusb and libusb1 if not already installed like `sudo apt-get libusb-dev` on a Rapsberry Pi
* Copy the [99-lego-dimensions.rules](./linux-driver-rules/99-lego-dimensions.rules) file into `/etc/udev/rules.d/`. This will allow you to run the code from any user.
* Reboot then device or apply the changes

### Portal instantiation and basic usage

The code supports as many portal you you want. Each portal will instantiate a unique class. 2 static helpers are available to get an array of portal:

```csharp
using LegoDimensions;

var portal = LegoPortal.GetFirstPortal();
portal.LegoTagEvent += PortalLegoTagEvent;

Console.WriteLine("Press any key to exit");
while (!Console.KeyAvailable)
{
    Thread.Sleep(1000);
}

portal.Dispose();
Console.WriteLine("End of test");
```

In the previous example, you can register to Tag events. Those are related to adding and removing Lego tag from the portal.

As an example, you can add this callback function:

```csharp
void PortalLegoTagEvent(object? sender, LegoTagEventArgs e)
{
    Console.WriteLine($"Tag is present: {e.Present} - UID: {BitConverter.ToString(e.CardUid)}");
    if (e.Present)
    {
        Console.WriteLine($"Tag is a {e.LegoTag?.GetType().Name} - Name= {e.LegoTag?.Name}");
    }
}
```

While placing and removing tags on the different pads, you'll get output like this:

```text
Tag is present: True - UID: 04-CA-8A-B2-6D-40-80
Tag is a Vehicle - Name= Scooby Snack
Tag is present: True - UID: 04-64-74-FA-00-49-81
Tag is a Character - Name= Ethan Hunt
Tag is present: False - UID: 04-64-74-FA-00-49-81
```

The event object contains more information like the index. The index and the pad are need once you want to do some specific operations.

## Lego Dimensions pad color adjustment

You can adjust the leds present in the portal. Most functions will allow you to adjust one or all pads at the same time.

### Set color immediately on a pad

The `SetColor` function allows you to set the color on one or all pads at the same time. You will find example bellow.

```csharp
Console.WriteLine("Pad left blue");
portal.SetColor(Pad.LeftPad, Color.Blue);
Thread.Sleep(1000);
Console.WriteLine("Pad center white");
portal.SetColor(Pad.CenterPad, Color.White);
Thread.Sleep(1000);
Console.WriteLine("Pad right purple");
portal.SetColor(Pad.RightPad, Color.Red);
Thread.Sleep(1000);
Console.WriteLine("All yellow");
portal.SetColor(Pad.AllPads, Color.Yellow);
```

### Get color displayed on a specific pad

You can as well check the color that is currently displayed on a specific pad.

```csharp
Console.WriteLine("All yellow");
portal.SetColor(Pad.AllPads, Color.Yellow);
Thread.Sleep(200);
var col = portal.GetColor(Pad.LeftPad);
Console.WriteLine($"Pad left color: {col.R}-{col.G}-{col.B}");
```

If for some reason, this function does not get a proper answer, the Black color is the one which will be returned.

### Set colors immediately on all pads

This `SetColorAll` function will set a specific color on all pads. Each pad can had a different color. The difference with te `SetColor` function is that on the `SetColor` one, if you are using all pads, they will have the same color.

```csharp
Console.WriteLine("Pad left Blue, center white, right red");
portal.SetColorAll(Color.White, Color.Blue, Color.Red);
```

### Flashing a pad

You can flash a pad or all at the same time with the same settings using the `Flash` function.

The FlashPad uses 3 settings:

* The tick on setting will count how much time it will stay on. The smallest, the fastest.
* The tick off setting will count how much time it will stay off. The smallest, the fastest.
* The tick count represent the number of change operation. So a full blink is actually 2 counts. So odd number will always finish on the previous color while the even ones on the new color.

```csharp
Console.WriteLine("Pad right azure flashing 20 times (10 on and 10 off)");
portal.Flash(Pad.RightPad, new FlashPad(20, 20, 20, Color.Azure));
```

### Flashing all pads

This `FlashAll` is similar to the `Flash` one except you can individually set a specific configuration for each pad. Settings are the same.

You have one additional setting: `Enabled` which allow to have the pad switched off or on.

```csharp
Console.WriteLine("Pad center red asymmetric flashing 40 times (20 on and 20 off)");
Console.WriteLine("Pad left green flashing forever times fast (10 on and 10 off)");
Console.WriteLine("Pad right azure flashing 20 times (10 on and 10 off)");
portal.FlashAll(new FlashPad(10, 30, 40, Color.Red), new FlashPad(1, 1, 255, Color.Green), new FlashPad(20, 20, 20, Color.Azure));
```

### Fading a pad

You can fade one or all pads at the same time. The fading speed is determined by the first parameter in the `FadePad` class and the count by the second. A count is a pulse, so either fading down either up, so you'll need 2 to have a full down then up. For the speed, the high the number is, the slowest is the pulse.

```csharp
Console.WriteLine("Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.");
portal.Fade(Pad.CenterPad, new FadePad(50, 5, Color.Red));
```

### Fading all pads

As for the `Fade`, you can use `FadeAll` to fade all pads at the same time. Each pad can have its own setting. Settings are the same. You have an additional one `Enabled` which allow to have the pad activated or not.

```csharp
Console.WriteLine("Fade center pad relatively slowly from the displayed color to red, will finish on the new color as odd number of count.");
Console.WriteLine("Fade left pad relatively quit fast from the displayed color to green, will finish on the old color as even number of count.");
Console.WriteLine("Fade right will not fade as disabled regardless of the values placed in the Fade Pad.");
portal.FadeAll(new FadePad(50, 5, Color.Red), new FadePad(5, 50, Color.Green), new FadePad(10, 100, Color.Yellow, false));
```

### Fading a pad to a random color

This can be used to fade a pad on a random color, the speed and the count are the same settings as for the other fade functions.

```csharp
Console.WriteLine("Randome fading on left pad");
portal.FadeRandom(Pad.LeftPad, 10, 10);
Thread.Sleep(1000);
Console.WriteLine("Randome fading on center pad");
portal.FadeRandom(Pad.CenterPad, 1, 100);
Thread.Sleep(1000);
Console.WriteLine("Randome fading on right pad");
portal.FadeRandom(Pad.RightPad, 5, 15);
Thread.Sleep(1000);
```

### Reading tags

You can read tags when present on the pad. The following example shows how you can dump a full tag:

```csharp
Console.WriteLine("Place at least 1 tag on the portal, this will dump the full tag.");
while (!portal.PresentTags.Any())
{
    Thread.Sleep(1000);
}

var idx = portal.PresentTags.First().Index;
for (byte i = 0; i < 0x2c; i += 4)
{
    var tag = portal.ReadTag(idx, i);
    if (tag.Length == 0)
    {
        Console.WriteLine($"Error reading card page 0x{i:X2}")
    }
    else
    {
        Console.WriteLine($"Tag: {BitConverter.ToString(tag)}");
    }
}
```

If there will be an error reading the card, you'll get an empty buffer. This can happen if you are moving the card of if you're using a non initialized card. See the section on the Lego Tags to understand how to initialize a card.

### Write a tag

To be documented later.

### List tags on the pad

You should keep track of the tags with their UID. But a list of available tab with their index and associated tag is available through the `PresentTags` property as well as when calling the `ListTags` function.

## Lego Tags

The [Lego Dimensions library](LegoDimensions) contains the full support to handle the Lego Tags. A full solution detecting the tags, reading them and displaying the characters and vehicles is available in the [Lego Dimensions Read NFC project](LegoDimensionsReadNfc).

The Read NFC project is currently using a [PN532](https://github.com/dotnet/iot/tree/main/src/devices/Pn532) connected in serial to read the tags. It's based on the [.NET IoT](https://github.com/dotnet/iot) implementation for both the reader and the Ultralight tags.

The example is complete and gives all the steps to:

* Connect the PN532
* Get a card, process the ultralight card
* Generate the authentication key (4 bytes) to read the protected data tags
* Read the pages 0x24, 0x25, 0x26 and 0x27 where the actual tag elements are stored
* Process and decrypt the vehicle or character information
* Display the vehicle or character information
* Do this over and over, detect new cards, handle issues with non supported cards or potential errors

You just need to adjust the serial port used (auto detection can be an improvement). Mind the name of the port that can be different depending on which operating system you'll run it. This code will work on Windows, Linux and MacOS wherever .NET 6.0 and further is supported.

Unit Tests are also provided to make sure any change will not break the encoding/decoding.

### Reading card data on an external NFC reader like a PN532

The ultralight NFC card is protected with password. This password is a 4 bytes. It can be processed with the `LegoTag` class with the `PasswordGenerator` function:

```csharp
Debug.WriteLine("Generating authentication key");
ultralight.AuthenticationKey = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
Debug.WriteLine($"Authentication key: {BitConverter.ToString(ultralight.AuthenticationKey)}");
ultralight.Command = UltralightCommand.PasswordAuthentication;
var auth = ultralight.RunUltralightCommand();

// read page 0x24 to 0x27
ultralight.BlockNumber = 0x24;
ultralight.Command = UltralightCommand.Read16Bytes;
var res = ultralight.RunUltralightCommand();
// Check we do have a result
if (res > 0)
{
    // Process the data
}
```

### Processing data

To process the data, you'll need a valid read from the previous section. It's a 16 byte buffer.

```csharp
// If page 0x26 == 00 01 00 00 we have a vehicle
if (LegoTag.IsVehicle(ultralight.Data.AsSpan(8, 4).ToArray()))
{
    Console.WriteLine("Found a vehicle.");
    // The 2 first one used
    var id = LegoTag.GetVehicleId(ultralight.Data);
    Console.Write($"vehicle ID: {id} ");
    Vehicle vec = Vehicle.Vehicles.FirstOrDefault(m => m.Id == id);
    if (vec is not null)
    {
        Console.WriteLine($"{vec.Name} - {vec.World}");
    }
    else
    {
        Console.WriteLine("and vehicle does not exist!");
    }
}
else
{
    Console.WriteLine("Found a character.");
    var id = LegoTag.GetCharacterId(ultralight.SerialNumber, ultralight.Data.AsSpan(0, 8).ToArray());
    Console.Write($"Character ID: {id} ");
    Character car = Character.Characters.FirstOrDefault(m => m.Id == id);
    if (car is not null)
    {
        Console.WriteLine($"{car.Name} - {car.World}");
    }
    else
    {
        Console.WriteLine("and character does not exist!");
    }
}
```

As a result, by placing different tags, you'll get something like:

``` text
Place the tag on the reader!
Found a vehicle.
vehicle ID: 123 Ghost Trap - Ghostbusters
Found a character.
Character ID: 42 Wicked Witch - Wizard of Oz
Found a character.
Character ID: 2 Gandalf - Lord of the Rings
Found a vehicle.
vehicle ID: 11 The Annihilator - The Lego Movie
```

### Writing data on a new NTAG213 card

The regular Lego Tag are protected and except the vehicle tag that can be written at any time, you can't change the character ID of a normal Lego tag. But you can write all this on a brand new tag.

If your tag is fully new, then you can just write whatever you want. You have the code in the [LegoDimensionsReadNfc sample](LegoDimensionsReadNfc).

In short, the important steps are the following:

1. Detect the tag
1. Create a new password based on the Card Unique ID (the 7 bytes) using the `LegoTag.GenerateCardPassword`
1. Write it to the 0x2B block on the card (assuming an NTAG213, address is different for 215 and 216)
1. If you want a vehicle:

    1. Use `LegoTag.EncryptVehicleId` to get the 4 bytes
    1. Write the 4 bytes on block 0x24
    1. Write `{ 0x00, 0x01, 0x00, 0x00 }` on block 0x26

1. If you want a character:

    1. Use `LegoTag.EncrypCharactertId` to get the 8 bytes
    1. Write the first 4 one on block 0x24
    1. Write the next 4 one on block 0x25
    1. If you've been using a vehicle before, write `{ 0x00, 0x00, 0x00, 0x00 }` on block 0x26

And now, you're all set! No need to adjust any other password!

## Sources of inspiration and code

* [LD-ToyPad-Emulator](https://github.com/Berny23/LD-ToyPad-Emulator)
* [liblegodimensionsportal](https://github.com/GrahamBriggs/liblegodimensionsportal)
* [lego_dimensions_protocol](https://github.com/woodenphone/lego_dimensions_protocol)
* [ldnfctags](https://github.com/phogar/ldnfctags)
* [jtoypad](https://github.com/e-amzallag/jtoypad)
