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
using Terminal.Gui;

namespace LegoDimensionsReadNfc
{
    public class NfcPn532
    {
        static private string _device = string.Empty;
        static private Pn532? _pn532;
        static private byte[] _currentCardUid = new byte[0];
        static private UltralightCard? _ultralight;
        static private DisplayInfo? _displayInfo;

        static private Pn532 Pn532
        {
            get => _pn532 ?? throw new InvalidOperationException("OpenComPort must be called first.");
            set => _pn532 = value;
        }

        static private UltralightCard Ultralight
        {
            get => _ultralight ?? throw new InvalidOperationException("No NFC card is selected.");
            set => _ultralight = value;
        }

        static private DisplayInfo DisplayInfo
        {
            get => _displayInfo ?? throw new InvalidOperationException("The display is not initialized.");
            set => _displayInfo = value;
        }

        static private byte[] SerialNumber => Ultralight.SerialNumber
            ?? throw new InvalidOperationException("The selected NFC card has no serial number.");

        static public void OpenComPort(string device)
        {
            _device = device;
            Pn532 = new Pn532(_device);
        }

        static public void ErraseTag()
        {
            bool reselected = false;
            bool stopped = false;

            Application.Init();
            DisplayInfo = new DisplayInfo();
            DisplayInfo.Label.Text = "Erase Tag: place an empty tag on the reader to erase it.";
            DisplayInfo.View.Text = string.Empty;
            DisplayInfo.ButtonClose.Clicked += () =>
            {
                stopped = true; ;
                Application.RequestStop();
            };

            (new Thread(() => Application.Run(DisplayInfo))).Start();

        Reselect:
            _currentCardUid = new byte[0];
            if (GetUltralightCard() == null)
            {
                return;
            }
            Debug.WriteLine($"Type: {Ultralight.UltralightCardType}, Ndef capacity: {Ultralight.NdefCapacity}");

            if (reselected)
            {
                var passwd = LegoTag.GenerateCardPassword(SerialNumber);
                if (!Ultralight.ProcessAuthentication(passwd))
                {
                    DisplayInfo.View.Text += "Failed to authenticate the card.\r\n";
                    return;
                }
            }

            // Try to read the car on 0x24 just to see if we need the password
            if (!ReadBlock(0x24))
            {
                if (!reselected)
                {
                    reselected = true;
                    goto Reselect;
                }

                if (!ReadBlock(0x24))
                {
                    DisplayInfo.View.Text += "Failed to read data block 0x24, can't erase the card.\r\n";
                    return;
                }
            }

            bool erased = true;
            for (int i = 0; i < 4; i++)
            {
                if (!WriteAndCheck((byte)(0x24 + i), new byte[4]))
                {
                    DisplayInfo.View.Text += $"Error erasing block 0x{(0x24 + i):X2}.\r\n";
                    erased = false;
                }
            }

            if (erased)
            {
                DisplayInfo.View.Text += "Card erased.\r\n";
            }
            else
            {
                DisplayInfo.View.Text += "Card may not have been erased properly.\r\n";
            }

            Application.DoEvents();

            while (!stopped)
            {
                Thread.Sleep(100);
            }

            Application.Shutdown();
        }

        static public void WriteEmptyTag(ushort id, bool character)
        {
            bool reselected = false;
            bool stopped = false;

            Application.Init();
            DisplayInfo = new DisplayInfo();
            DisplayInfo.Label.Text = "Write Tag: place an empty tag on the reader to write it.";
            DisplayInfo.View.Text = string.Empty;
            DisplayInfo.ButtonClose.Clicked += () =>
            {
                stopped = true; ;
                Application.RequestStop();
            };

            (new Thread(() => Application.Run(DisplayInfo))).Start();

        Reselect:
            _currentCardUid = new byte[0];
            if (GetUltralightCard() == null)
            {
                return;
            }
            Debug.WriteLine($"Type: {Ultralight.UltralightCardType}, Ndef capacity: {Ultralight.NdefCapacity}");

            // First step: change the password
            // Process the password
            var passwd = LegoTag.GenerateCardPassword(SerialNumber);
            DisplayInfo.View.Text += $"Card {BitConverter.ToString(SerialNumber)}, generated password: {BitConverter.ToString(passwd)}\r\n";

            if (reselected)
            {
                if (!Ultralight.ProcessAuthentication(passwd))
                {
                    DisplayInfo.View.Text += "Failed to authenticate the card.\r\n";
                    Application.RequestStop();
                    return;
                }
            }

            // Try to read the car on 0x24 just to see if we need the password

            if (!ReadBlock(0x24))
            {
                if (!reselected)
                {
                    reselected = true;
                    goto Reselect;
                }

                if (Ultralight.RunUltralightCommand() < 0)
                {
                    DisplayInfo.View.Text += "Failed to read data block 0x24, can't erase the card.\r\n";
                    Application.RequestStop();
                    return;
                }
            }

            if (!reselected)
            {
                // 0x2B for NTAG213
                if (Ultralight.UltralightCardType == UltralightCardType.UltralightNtag213)
                {
                    Ultralight.BlockNumber = 0x2B;
                }
                else if (Ultralight.UltralightCardType == UltralightCardType.UltralightNtag215)
                {
                    Ultralight.BlockNumber = 0x85;
                }
                else if (Ultralight.UltralightCardType == UltralightCardType.UltralightNtag216)
                {
                    Ultralight.BlockNumber = 0xE5;
                }
                else
                {
                    DisplayInfo.View.Text += $"Not a supported NTAG card, only 213, 215 and 216 ar supported. Current detected type: {Ultralight.UltralightCardType}.\r\n";
                    Application.RequestStop();
                    return;
                }

                Ultralight.Command = UltralightCommand.Write4Bytes;
                Ultralight.Data = passwd;
                if (Ultralight.RunUltralightCommand() < 0)
                {
                    DisplayInfo.View.Text += "Failed to write new password\r\n";
                    Application.RequestStop();
                    return;
                }

                DisplayInfo.View.Text += "New password set.\r\n";
            }
            else
            {
                DisplayInfo.View.Text += "Password already set\r\n";
            }

            int retry = 0;
            // Second step: write the data
            if (character)
            {
                // Get the encrypted character ID
                var car = LegoTag.EncrypCharactertId(SerialNumber, id);

                if (!WriteAndCheck(0x24, car.AsSpan(0, 4).ToArray()))
                {
                    DisplayInfo.View.Text += "Most likely failed to write character as can't check block 0x24.\r\n";
                }

                if (!WriteAndCheck(0x25, car.AsSpan(4, 4).ToArray()))
                {
                    DisplayInfo.View.Text += "Most likely failed to write character as can't check block 0x25.\r\n";
                }
            }
            else
            {

                // Get the encrypted vehicle ID
                var vec = LegoTag.EncryptVehicleId(id);
                if (!WriteAndCheck(0x24, vec))
                {
                    DisplayInfo.View.Text += "Most likely failed to write vehicle as can't check block 0x24.\r\n";
                }

                // Then write it's a vehicle
                if (!WriteAndCheck(0x26, new byte[] { 0x00, 0x01, 0x00, 0x00 }))
                {
                    DisplayInfo.View.Text += "Most likely failed to write vehicle as can't check block 0x26.\r\n";
                }
            }

            DisplayInfo.View.Text += "Setup for the new card done.\r\n";

            Application.DoEvents();

            while (!stopped)
            {
                Thread.Sleep(100);
            }

            Application.Shutdown();
        }

        static public void ReadLegoTag(bool dump)
        {
            bool stopped = false;

            Application.Init();
            DisplayInfo = new DisplayInfo();
            DisplayInfo.Label.Text = "Write Tag: place an empty tag on the reader to write it.";
            DisplayInfo.View.Text = string.Empty;
            DisplayInfo.ButtonClose.Clicked += () =>
            {
                stopped = true; ;
                Application.RequestStop();
            };

            (new Thread(() => Application.Run(DisplayInfo))).Start();

            try
            {
                _currentCardUid = new byte[0];
                if (GetUltralightCard() == null)
                {
                    return;
                }

                Debug.WriteLine($"Type: {Ultralight.UltralightCardType}, Ndef capacity: {Ultralight.NdefCapacity}");

                // For debug purposes, you can uncomment
                if (dump)
                {
                    DisplayVersion();
                }

                // Try authentication
                Debug.WriteLine("Generating authentication key");
                Ultralight.AuthenticationKey = LegoTag.GenerateCardPassword(SerialNumber);
                DisplayInfo.View.Text += $"Authentication key: {BitConverter.ToString(Ultralight.AuthenticationKey)}.\r\n";
                Application.DoEvents();
                Ultralight.Command = UltralightCommand.PasswordAuthentication;
                var auth = Ultralight.RunUltralightCommand();

                // For debug pu^rposes, you can display all page card
                if (dump)
                {
                    ReadAllCard();
                }

                // read page 0x24   
                if (ReadBlock(0x24))
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Debug.Write($"{Ultralight.Data![i]:X2} ");
                    }

                    // If page 0x26 == 00 01 00 00 we have a vehicle
                    if (LegoTag.IsVehicle(Ultralight.Data.AsSpan(8, 4).ToArray()))
                    {
                        DisplayInfo.View.Text += "Found a vehicle.\r\n";
                        // The 2 first one used
                        var id = LegoTag.GetVehicleId(Ultralight.Data);
                        DisplayInfo.View.Text += $"  vehicle ID: {id}: ";
                        Vehicle? vec = Vehicle.Vehicles.FirstOrDefault(m => m.Id == id);
                        if (vec is not null)
                        {
                            DisplayInfo.View.Text += $"{vec.Name}, {vec.Rebuild} build - {vec.World}.\r\n";
                            DisplayInfo.View.Text += "  Capabilities: ";
                            for (int i = 0; i < vec.Abilities.Count; i++)
                            {
                                DisplayInfo.View.Text += $"{vec.Abilities[i]}{(i != vec.Abilities.Count - 1 ? ", " : "")}";
                            }

                            DisplayInfo.View.Text += "\r\n";
                            Application.DoEvents();
                        }
                        else
                        {
                            DisplayInfo.View.Text += "and vehicle does not exist!\r\n";
                            Application.DoEvents();
                        }
                    }
                    else
                    {
                        DisplayInfo.View.Text += "Found a character.\r\n";
                        var id = LegoTag.GetCharacterId(SerialNumber, Ultralight.Data.AsSpan(0, 8).ToArray());
                        DisplayInfo.View.Text += $"  Character ID: {id}: ";
                        Character? car = Character.Characters.FirstOrDefault(m => m.Id == id);
                        if (car is not null)
                        {
                            DisplayInfo.View.Text += $"{car.Name} - {car.World}.\r\n";
                            DisplayInfo.View.Text += "  Capabilities: ";
                            for (int i = 0; i < car.Abilities.Count; i++)
                            {
                                DisplayInfo.View.Text += $"{car.Abilities[i]}{(i != car.Abilities.Count - 1 ? ", " : "")}";
                            }

                            DisplayInfo.View.Text += "\r\n";
                            Application.DoEvents();
                        }
                        else
                        {
                            DisplayInfo.View.Text += "and character does not exist!\r\n";
                            Application.DoEvents();
                        }
                    }
                }
                else
                {
                    _currentCardUid = new byte[0];
                    DisplayInfo.View.Text += "Can't read the tag, place it again or another one.\r\n";
                }
            }
            catch (Exception)
            {
                _currentCardUid = new byte[0];
                DisplayInfo.View.Text += "Can't read the tag, place it again or another one.\r\n";
            }

            Application.DoEvents();
            while (!stopped)
            {
                Thread.Sleep(100);
            }

            Application.Shutdown();
        }

        static public void ReadAllCard()
        {
            DisplayInfo.View.Text += "Dump of all the card:\r\n";
            Application.DoEvents();
            for (int block = 0; block < Ultralight.NumberBlocks; block++)
            {
                if (ReadBlock((byte)block))
                {
                    DisplayInfo.View.Text += $"  Block: {Ultralight.BlockNumber:X2} - ";
                    for (int i = 0; i < 4; i++)
                    {
                        DisplayInfo.View.Text += $"{Ultralight.Data![i]:X2} ";
                    }

                    var isReadOnly = Ultralight.IsPageReadOnly(Ultralight.BlockNumber);
                    DisplayInfo.View.Text += $"- Read only: {isReadOnly} ";

                    DisplayInfo.View.Text += "\r\n";
                }
                else
                {
                    DisplayInfo.View.Text += "Can't read card\r\n";
                }

                Application.DoEvents();
            }
        }

        static public void DisplayVersion()
        {
            var version = Ultralight.GetVersion();
            if ((version != null) && (version.Length > 0))
            {
                DisplayInfo.View.Text += "Get Version details: ";
                for (int i = 0; i < version.Length; i++)
                {
                    DisplayInfo.View.Text += $"{version[i]:X2} ";
                }

                DisplayInfo.View.Text += "\r\n";
            }
            else
            {
                DisplayInfo.View.Text += "Can't read the version.\r\n";
            }

            Application.DoEvents();
        }

        static private bool WriteAndCheck(byte block, byte[] data, int maxRetries = 3)
        {
            int retry = 0;
        RetryWrite:
            if (Ultralight.IsPageReadOnly(block))
            {
                DisplayInfo.View.Text += $"Block 0x{block:X2} is readonly.\r\n";
                return false;
            }

            if (!WriteBlock(block, data))
            {
                if (retry++ < maxRetries)
                {
                    // Try to auth
                    Auth();
                    Thread.Sleep(100);
                    goto RetryWrite;
                }

                DisplayInfo.View.Text += $"Failed to write block 0x{block:X2} after {maxRetries}.\r\n";
                return false;
            }
            else
            {
                // Time for the data to be properly written
                Thread.Sleep(100);
                retry = 0;
            RetryRead:
                Ultralight.Data = new byte[0];
                // Check it's correct
                if (!ReadBlock(block))
                {
                    if (retry++ < maxRetries)
                    {
                        // Try to auth
                        Auth();
                        Thread.Sleep(100);
                        goto RetryRead;
                    }

                    DisplayInfo.View.Text += $"Can't check block 0x{block:X2}.\r\n";
                    return false;
                }

                if (!data.SequenceEqual(Ultralight.Data.AsSpan(0, 4).ToArray()))
                {
                    if (retry++ < maxRetries)
                    {
                        Thread.Sleep(100);
                        goto RetryWrite;
                    }

                    DisplayInfo.View.Text += $"Can't validate block 0x{block:X2} even after {maxRetries} retries.\r\n";
                    return false;
                }
            }

            return true;
        }

        static private bool Auth()
        {
            var passwd = LegoTag.GenerateCardPassword(SerialNumber);
            Ultralight.Data = passwd;
            Ultralight.Command = UltralightCommand.PasswordAuthentication;
            var res = Ultralight.RunUltralightCommand();
            return res >= 0;
        }

        static private bool WriteBlock(byte block, byte[] data)
        {
            Ultralight.Data = data;
            Ultralight.BlockNumber = block;
            Ultralight.Command = UltralightCommand.Write4Bytes;
            return Ultralight.RunUltralightCommand() >= 0;
        }

        static private bool ReadBlock(byte block)
        {
            Ultralight.BlockNumber = (byte)block; // Safe cast, can't be more than 255
            Ultralight.Command = UltralightCommand.Read16Bytes;
            var res = Ultralight.RunUltralightCommand();
            return res >= 0;
        }

        static private UltralightCard? GetUltralightCard()
        {
        CheckCard:
            byte[]? retData = null;
            while ((!Console.KeyAvailable))
            {
                retData = Pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
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
            var decrypted = Pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

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

                Ultralight = new UltralightCard(Pn532, 0);
                Ultralight.SerialNumber = decrypted.NfcId;
                return Ultralight;
            }

            return null;
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
    }
}
