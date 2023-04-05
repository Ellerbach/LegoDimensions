// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using Iot.Device.Card.CreditCardProcessing;
using Iot.Device.Card.Ultralight;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using LegoDimensions;
using System.Diagnostics;

Console.WriteLine("Place the empty NTAG21x on the reader!");

string device = "COM10";
var pn532 = new Pn532(device);
byte[] currentCardUid = new byte[0];

// If you want to read a tag, uncomment the following line
 ReadLegoTag();

// If you want to write an empty tag, uncomment the following line
// Use any Character ID
// Here it will be E.T.
//WriteEmptyTagToCharacter(61, true);
// Here write a vehicle 166, Slime Streamer
//WriteEmptyTagToCharacter(166, false);

void ErraseTag()
{
    byte[] retData = null;
    while ((!Console.KeyAvailable))
    {
        retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
        if (retData is object)
        {
            break;
        }

        // Give time to PN532 to process
        Thread.Sleep(200);
        currentCardUid = new byte[0];
    }

    // Key pressed, exit
    if (retData is null)
    {
        return;
    }

    // You need to remove the first element at it's the number of tags read
    // In, this case we will assume we are reading only 1 tag at a time
    var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

    if (decrypted is object)
    {
        Debug.WriteLine($"Tg: {decrypted.TargetNumber}, ATQA: {decrypted.Atqa} SAK: {decrypted.Sak}, NFCID: {BitConverter.ToString(decrypted.NfcId)}");
        if (decrypted.Ats is object)
        {
            Debug.WriteLine($", ATS: {BitConverter.ToString(decrypted.Ats)}");
        }

        currentCardUid = decrypted.NfcId;

        var ultralight = new UltralightCard(pn532, 0);
        ultralight.SerialNumber = decrypted.NfcId;
        Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

        // If you haven't change anything to the card and it's ok to read any block
        // without the password, you don't need this
        //var passwd = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
        //if (!ultralight.ProcessAuthentication(passwd))
        //{
        //    Console.WriteLine("Failed to authenticate with new key.");
        //    return;
        //}

        ultralight.Data = new byte[4];
        for (int i = 0; i < 4;)
        {
            ultralight.BlockNumber = (byte)(0x24 + i);
            ultralight.Command = UltralightCommand.Write4Bytes;
            if (ultralight.RunUltralightCommand() < 0)
            {
                Console.WriteLine($"Failed to write data block {(0x24 + i):X2}");
                return;
            }
        }
    }
}

void WriteEmptyTag(ushort id, bool character)
{    
    byte[] retData = null;
    while ((!Console.KeyAvailable))
    {
        retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
        if (retData is object)
        {
            break;
        }

        // Give time to PN532 to process
        Thread.Sleep(200);
        currentCardUid = new byte[0];
    }

    // Key pressed, exit
    if (retData is null)
    {
        return;
    }

    // You need to remove the first element at it's the number of tags read
    // In, this case we will assume we are reading only 1 tag at a time
    var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

    if (decrypted is object)
    {
        Debug.WriteLine($"Tg: {decrypted.TargetNumber}, ATQA: {decrypted.Atqa} SAK: {decrypted.Sak}, NFCID: {BitConverter.ToString(decrypted.NfcId)}");
        if (decrypted.Ats is object)
        {
            Debug.WriteLine($", ATS: {BitConverter.ToString(decrypted.Ats)}");
        }

        currentCardUid = decrypted.NfcId;

        var ultralight = new UltralightCard(pn532, 0);
        ultralight.SerialNumber = decrypted.NfcId;
        Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

        // First step: change the password
        // Process the password
        var passwd = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
        Console.WriteLine($"Card {BitConverter.ToString(ultralight.SerialNumber)}, generated password: {BitConverter.ToString(passwd)}");
        
        // If you haven't change anything to the card and it's ok to read any block
        // without the password, you don't need this
        //if (!ultralight.ProcessAuthentication(ultralight.AuthenticationKey))
        //{
        //    Console.WriteLine("Failed to authenticate with default key.");
        //    return;
        //}

        // 0x2B for NTAG213
        ultralight.BlockNumber = 0x2B;
        ultralight.Command = UltralightCommand.Write4Bytes;
        ultralight.Data = passwd;
        if (ultralight.RunUltralightCommand() < 0)
        {
            Console.WriteLine("Failed to write new password");
            return;
        }

        // If you haven't change anything to the card and it's ok to read any block
        // without the password, you don't need this
        //if (!ultralight.ProcessAuthentication(passwd))
        //{
        //    Console.WriteLine("Failed to authenticate with new key.");
        //    return;
        //}

        Console.WriteLine("New password set.");

        // Second step: write the data
        if (character)
        {
            // Get the encrypted character ID
            var car = LegoTag.EncrypCharactertId(ultralight.SerialNumber, id);
            ultralight.Data = car.AsSpan(0, 4).ToArray();
            ultralight.BlockNumber = 0x24;
            ultralight.Command = UltralightCommand.Write4Bytes;
            if (ultralight.RunUltralightCommand() < 0)
            {
                Console.WriteLine("Failed to write bloc 0x24");
                return;
            }

            ultralight.Data = car.AsSpan(4, 4).ToArray();
            ultralight.BlockNumber = 0x25;
            ultralight.Command = UltralightCommand.Write4Bytes;
            if (ultralight.RunUltralightCommand() < 0)
            {
                Console.WriteLine("Failed to write bloc 0x25");
                return;
            }
        }
        else
        {
            // Get the encrypted vehicle ID
            var vec = LegoTag.EncryptVehicleId(id);
            ultralight.Data = vec;
            ultralight.BlockNumber = 0x24;
            ultralight.Command = UltralightCommand.Write4Bytes;
            if (ultralight.RunUltralightCommand() < 0)
            {
                Console.WriteLine("Failed to write bloc 0x24");
                return;
            }

            // Then write it's a vehicle
            ultralight.Data = new byte[] { 0x00, 0x01, 0x00, 0x00 };
            ultralight.BlockNumber = 0x26;
            ultralight.Command = UltralightCommand.Write4Bytes;
            if (ultralight.RunUltralightCommand() < 0)
            {
                Console.WriteLine("Failed to write bloc 0x26");
                return;
            }
        }

        Console.WriteLine("Setup for the new card done");
    }
}

void ReadLegoTag()
{

CheckCard:

    try
    {
        byte[] retData = null;
        while ((!Console.KeyAvailable))
        {
            retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
            if (retData is object)
            {
                break;
            }

            // Give time to PN532 to process
            Thread.Sleep(200);
            currentCardUid = new byte[0];
        }

        // Key pressed, exit
        if (retData is null)
        {
            return;
        }

        // You need to remove the first element at it's the number of tags read
        // In, this case we will assume we are reading only 1 tag at a time
        var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

        if (decrypted is object)
        {
            if (currentCardUid.SequenceEqual(decrypted.NfcId))
            {
                Thread.Sleep(1000);
                goto CheckCard;
            }

            Debug.WriteLine($"Tg: {decrypted.TargetNumber}, ATQA: {decrypted.Atqa} SAK: {decrypted.Sak}, NFCID: {BitConverter.ToString(decrypted.NfcId)}");
            if (decrypted.Ats is object)
            {
                Debug.WriteLine($", ATS: {BitConverter.ToString(decrypted.Ats)}");
            }

            currentCardUid = decrypted.NfcId;

            var ultralight = new UltralightCard(pn532, 0);
            ultralight.SerialNumber = decrypted.NfcId;
            Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

            // For debug purposes, you can uncomment
            ////DisplayVersion(ultralight);

            // Try authentication
            Debug.WriteLine("Generating authentication key");
            ultralight.AuthenticationKey = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
            Debug.WriteLine($"Authentication key: {BitConverter.ToString(ultralight.AuthenticationKey)}");
            ultralight.Command = UltralightCommand.PasswordAuthentication;
            var auth = ultralight.RunUltralightCommand();

            // For debug pu^rposes, you can display all page card
            ReadAllCard(ultralight);

            // read page 0x24   
            ultralight.BlockNumber = 0x24;
            ultralight.Command = UltralightCommand.Read16Bytes;
            var res = ultralight.RunUltralightCommand();
            // Check we do have a result
            if (res > 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    Debug.Write($"{ultralight.Data![i]:X2} ");
                }

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
            }
            else
            {
                currentCardUid = new byte[0];
                Console.WriteLine("Can't read the tag, place it again or another one");
            }
        }
    }
    catch (Exception)
    {
        currentCardUid = new byte[0];
        Console.WriteLine("Can't read the tag, place it again or another one");
    }

    Thread.Sleep(1000);
    goto CheckCard;
}

void ReadAllCard(UltralightCard ultralight)
{
    Console.WriteLine("Dump of all the card:");
    for (int block = 0; block < ultralight.NumberBlocks; block++)
    {
        ultralight.BlockNumber = (byte)block; // Safe cast, can't be more than 255
        ultralight.Command = UltralightCommand.Read16Bytes;
        var res = ultralight.RunUltralightCommand();
        if (res > 0)
        {
            Console.Write($"  Block: {ultralight.BlockNumber:X2} - ");
            for (int i = 0; i < 4; i++)
            {
                Console.Write($"{ultralight.Data![i]:X2} ");
            }

            var isReadOnly = ultralight.IsPageReadOnly(ultralight.BlockNumber);
            Console.Write($"- Read only: {isReadOnly} ");

            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("Can't read card");
            //break;
        }
    }
}

void DisplayVersion(UltralightCard ultralight)
{
    var version = ultralight.GetVersion();
    if ((version != null) && (version.Length > 0))
    {
        Console.WriteLine("Get Version details: ");
        for (int i = 0; i < version.Length; i++)
        {
            Console.Write($"{version[i]:X2} ");
        }

        Console.WriteLine();
    }
    else
    {
        Console.WriteLine("Can't read the version.");
    }
}