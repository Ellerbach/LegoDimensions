using Iot.Device.BoardLed;
using Iot.Device.Card.Ultralight;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using LegoDimensions.Tag;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsReadNfc
{
    public class NfcPn532
    {
        static private string _device;
        static private Pn532 _pn532;
        static private byte[] _currentCardUid = new byte[0];

        static public void OpenComPort(string device)
        {
            _device = device;
            _pn532 = new Pn532(_device);
        }

        static public void ErraseTag()
        {
            bool reselected = false;

        Reselect:
            var ultralight = GetUltralightCard();
            Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

            if (reselected)
            {
                var passwd = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
                if (!ultralight.ProcessAuthentication(passwd))
                {
                    Console.WriteLine("Failed to authenticate the card.");
                    return;
                }
            }

            // Try to read the car on 0x24 just to see if we need the password
            if (!ReadBlock(ultralight, 0x24))
            {
                if (!reselected)
                {
                    reselected = true;
                    goto Reselect;
                }

                if (!ReadBlock(ultralight, 0x24))
                {
                    Console.WriteLine("Failed to read data block 0x24, can't erase the card.");
                    return;
                }
            }

            bool erased = true;
            for (int i = 0; i < 4; i++)
            {
                if (!WriteAndCheck(ultralight, (byte)(0x24 + i), new byte[4]))
                {
                    Console.WriteLine($"Error erasing block 0x{(0x24 + i):X2}.");
                    erased = false;
                }
            }

            if (erased)
            {
                Console.WriteLine("Card erased.");
            }
            else
            {
                Console.WriteLine("Card may not have been erased properly.");
            }
        }

        static public void WriteEmptyTag(ushort id, bool character)
        {
            bool reselected = false;

        Reselect:
            var ultralight = GetUltralightCard();
            Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

            // First step: change the password
            // Process the password
            var passwd = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
            Console.WriteLine($"Card {BitConverter.ToString(ultralight.SerialNumber)}, generated password: {BitConverter.ToString(passwd)}");

            if (reselected)
            {
                if (!ultralight.ProcessAuthentication(passwd))
                {
                    Console.WriteLine("Failed to authenticate the card.");
                    return;
                }
            }

            // Try to read the car on 0x24 just to see if we need the password

            if (!ReadBlock(ultralight, 0x24))
            {
                if (!reselected)
                {
                    reselected = true;
                    goto Reselect;
                }

                if (ultralight.RunUltralightCommand() < 0)
                {
                    Console.WriteLine("Failed to read data block 0x24, can't erase the card.");
                    return;
                }
            }

            if (!reselected)
            {
                // 0x2B for NTAG213
                if (ultralight.UltralightCardType == UltralightCardType.UltralightNtag213)
                {
                    ultralight.BlockNumber = 0x2B;
                }
                else if (ultralight.UltralightCardType == UltralightCardType.UltralightNtag215)
                {
                    ultralight.BlockNumber = 0x85;
                }
                else if (ultralight.UltralightCardType == UltralightCardType.UltralightNtag216)
                {
                    ultralight.BlockNumber = 0xE5;
                }
                else
                {
                    Console.WriteLine($"Not a supported NTAG card, only 213, 215 and 216 ar supported. Current detected type: {ultralight.UltralightCardType}");
                    return;
                }

                ultralight.Command = UltralightCommand.Write4Bytes;
                ultralight.Data = passwd;
                if (ultralight.RunUltralightCommand() < 0)
                {
                    Console.WriteLine("Failed to write new password");
                    return;
                }

                Console.WriteLine("New password set.");
            }
            else
            {
                Console.WriteLine("Password already set");
            }

            int retry = 0;
            // Second step: write the data
            if (character)
            {
                // Get the encrypted character ID
                var car = LegoTag.EncrypCharactertId(ultralight.SerialNumber, id);

                if (!WriteAndCheck(ultralight, 0x24, car.AsSpan(0, 4).ToArray()))
                {
                    Console.WriteLine("Most likely failed to write character as can't check block 0x24.");
                }

                if (!WriteAndCheck(ultralight, 0x25, car.AsSpan(4, 4).ToArray()))
                {
                    Console.WriteLine("Most likely failed to write character as can't check block 0x25.");
                }
            }
            else
            {

                // Get the encrypted vehicle ID
                var vec = LegoTag.EncryptVehicleId(id);
                if (!WriteAndCheck(ultralight, 0x24, vec))
                {
                    Console.WriteLine("Most likely failed to write vehicle as can't check block 0x24.");
                }

                // Then write it's a vehicle
                if (!WriteAndCheck(ultralight, 0x26, new byte[] { 0x00, 0x01, 0x00, 0x00 }))
                {
                    Console.WriteLine("Most likely failed to write vehicle as can't check block 0x26.");
                    return;
                }
            }

            Console.WriteLine("Setup for the new card done");
        }

        static public void ReadLegoTag(bool dump)
        {
            try
            {
                var ultralight = GetUltralightCard();

                Debug.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

                // For debug purposes, you can uncomment
                if (dump)
                {
                    DisplayVersion(ultralight);
                }

                // Try authentication
                Debug.WriteLine("Generating authentication key");
                ultralight.AuthenticationKey = LegoTag.GenerateCardPassword(ultralight.SerialNumber);
                Debug.WriteLine($"Authentication key: {BitConverter.ToString(ultralight.AuthenticationKey)}");
                ultralight.Command = UltralightCommand.PasswordAuthentication;
                var auth = ultralight.RunUltralightCommand();

                // For debug pu^rposes, you can display all page card
                if (dump)
                {
                    ReadAllCard(ultralight);
                }

                // read page 0x24   
                if (ReadBlock(ultralight, 0x24))
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
                            Console.WriteLine($"{vec.Id}: {vec.Name}, {vec.Rebuild} build - {vec.World}");
                            Console.Write("Capabilities: ");
                            for (int i = 0; i < vec.Abilities.Count; i++)
                            {
                                Console.Write($"{vec.Abilities[i]}{(i != vec.Abilities.Count - 1 ? ", " : "")}");
                            }

                            Console.WriteLine($"");
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
                            Console.WriteLine($"{car.Id}: {car.Name} - {car.World}");
                            for (int i = 0; i < car.Abilities.Count; i++)
                            {
                                Console.Write($"{car.Abilities[i]}{(i != car.Abilities.Count - 1 ? ", " : "")}");
                            }

                            Console.WriteLine($"");
                        }
                        else
                        {
                            Console.WriteLine("and character does not exist!");
                        }
                    }
                }
                else
                {
                    _currentCardUid = new byte[0];
                    Console.WriteLine("Can't read the tag, place it again or another one");
                }
            }
            catch (Exception)
            {
                _currentCardUid = new byte[0];
                Console.WriteLine("Can't read the tag, place it again or another one");
            }
        }

        static public void ReadAllCard(UltralightCard ultralight)
        {
            Console.WriteLine("Dump of all the card:");
            for (int block = 0; block < ultralight.NumberBlocks; block++)
            {
                if (ReadBlock(ultralight, (byte)block))
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

        static public void DisplayVersion(UltralightCard ultralight)
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

        static private bool WriteAndCheck(UltralightCard ultralight, byte block, byte[] data, int maxRetries = 3)
        {
            int retry = 0;
        RetryWrite:
            if (ultralight.IsPageReadOnly(block))
            {
                Console.WriteLine($"Block 0x{block:X2} is readonly.");
                return false;
            }

            if (!WriteBlock(ultralight, block, data))
            {
                if (retry++ < maxRetries)
                {
                    Thread.Sleep(100);
                    goto RetryWrite;
                }

                Console.WriteLine($"Failed to write block 0x{block:X2} after {maxRetries}.");
                return false;
            }
            else
            {
            RetryRead:
                ultralight.Data = new byte[0];
                // Check it's correct
                if (!ReadBlock(ultralight, block))
                {
                    if (retry++ < maxRetries)
                    {
                        Thread.Sleep(100);
                        goto RetryRead;
                    }

                    Console.WriteLine($"Can't check block 0x{block:X2}.");
                    return false;
                }

                if (!data.SequenceEqual(ultralight.Data))
                {
                    if (retry++ < maxRetries)
                    {
                        Thread.Sleep(100);
                        goto RetryWrite;
                    }

                    Console.WriteLine($"Can't validate block 0x{block:X2} even after {maxRetries} retries.");
                    return false;
                }
            }

            return true;
        }

        static private bool WriteBlock(UltralightCard ultralight, byte block, byte[] data)
        {
            ultralight.Data = data;
            ultralight.BlockNumber = block;
            ultralight.Command = UltralightCommand.Write4Bytes;
            return ultralight.RunUltralightCommand() >= 0;
        }

        static private bool ReadBlock(UltralightCard ultralight, byte block)
        {
            ultralight.BlockNumber = (byte)block; // Safe cast, can't be more than 255
            ultralight.Command = UltralightCommand.Read16Bytes;
            var res = ultralight.RunUltralightCommand();
            return res >= 0;
        }

        static private UltralightCard GetUltralightCard()
        {
        CheckCard:
            byte[] retData = null;
            while ((!Console.KeyAvailable))
            {
                retData = _pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                if (retData is object)
                {
                    break;
                }

                // Give time to PN532 to process
                Thread.Sleep(200);
                _currentCardUid = new byte[0];
            }

            // Key pressed, exit
            if (retData is null)
            {
                return null;
            }

            // You need to remove the first element at it's the number of tags read
            // In, this case we will assume we are reading only 1 tag at a time
            var decrypted = _pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

            if (decrypted is object)
            {
                Debug.WriteLine($"Tg: {decrypted.TargetNumber}, ATQA: {decrypted.Atqa} SAK: {decrypted.Sak}, NFCID: {BitConverter.ToString(decrypted.NfcId)}");
                if (decrypted.Ats is object)
                {
                    Debug.WriteLine($", ATS: {BitConverter.ToString(decrypted.Ats)}");
                }

                if (_currentCardUid.SequenceEqual(decrypted.NfcId))
                {
                    Thread.Sleep(1000);
                    goto CheckCard;
                }

                _currentCardUid = decrypted.NfcId;

                var ultralight = new UltralightCard(_pn532, 0);
                ultralight.SerialNumber = decrypted.NfcId;
                return ultralight;
            }

            return null;
        }
    }
}
