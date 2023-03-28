# Lego Dimensions portal and tag .NET implementation

The aim of this work is to be able to use Lego Dimensions portal and tags outside of their original purpose. Idea is to automate the usage or the portal led, but as well be able to read tags, with or without the portal to automate other external elements from a simple Raspberry PI for example.

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

### Reading card data

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
// If page 0x26 == 00 01 00 00 we have a vehicule
if (LegoTag.IsVehicle(ultralight.Data.AsSpan(8, 4).ToArray()))
{
    Console.WriteLine("Found a vehicle.");
    // The 2 first one used
    var id = LegoTag.GetVehiculeId(ultralight.Data);
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

## Portal interaction

This is currently under development.
