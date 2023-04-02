// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Diagnostics;

namespace LegoDimensions
{
    /// <summary>
    /// Instance of a Lego Dimensions Portal.
    /// </summary>
    public class LegoPortal : IDisposable
    {
        // Constants
        private const int ProductId = 0x0241;
        private const int VendorId = 0x0E6F;
        private const int ReadWriteTimeout = 1000;

        // Class variables
        private IUsbDevice _portal;
        private UsbEndpointReader _endpointReader;
        private UsbEndpointWriter _endpointWriter;
        private byte _messageId;
        private Thread _readThread;
        private CancellationTokenSource _cancelThread;

        // We do have only 3 Pads
        // This one is to store the last message ID request for details
        private byte[] _tagMessageId = new byte[3];
        // We'll keep track of the UUID and the character
        private LegoTagEventArgs[] _legoTag = new LegoTagEventArgs[3];

        /// <summary>
        /// Gets the first of default Lego Dimensions Portal.
        /// </summary>
        /// <returns>A Lego Dimensions Portal</returns>
        public static LegoPortal GetFirstPortal() => new LegoPortal(GetPortals()[0]);

        /// <summary>
        /// Gets all the available USB device that matches the Lego Dimensions Portal.
        /// </summary>
        /// <returns>Array of USB devices.</returns>
        public static IUsbDevice[] GetPortals()
        {
            var context = new UsbContext();
            context.SetDebugLevel(LogLevel.Info);

            //Get a list of all connected devices
            var usbDeviceCollection = context.List();

            //Narrow down the device by vendor and pid
            var selectedDevice = usbDeviceCollection.Where(d => d.ProductId == ProductId && d.VendorId == VendorId);

            return selectedDevice.ToArray();
        }

        public event EventHandler<LegoTagEventArgs>? LegoTagEvent;

        /// <summary>
        /// Creates a new instance of a Lego Dimensions Portal.
        /// </summary>
        /// <param name="device">A valid Lego Dimensions instance.</param>
        public LegoPortal(IUsbDevice device)
        {
            _portal = device;
            //Open the device
            _portal.Open();

            //Get the first config number of the interface
            _portal.ClaimInterface(_portal.Configs[0].Interfaces[0].Number);

            //Open up the endpoints
            _endpointWriter = _portal.OpenEndpointWriter(WriteEndpointID.Ep01);
            _endpointReader = _portal.OpenEndpointReader(ReadEndpointID.Ep01);

            // Read the first 32 bytes
            var readBuffer = new byte[32];
            _endpointReader.Read(readBuffer, ReadWriteTimeout, out var bytesRead);

            // Start the read thread
            _cancelThread = new CancellationTokenSource();
            _readThread = new Thread(ReadThread);
            _readThread.Start();
            // WakeUp the portal
            WakeUp();
        }

        /// <summary>
        /// Wakes up the portal.
        /// </summary>
        public void WakeUp()
        {
            Message message = new Message(MessageCommand.Wake);
            message.AddPayload("(c) LEGO 2014");
            _messageId = 1;
            SendMessage(message);
        }

        /// <summary>
        /// Sets a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to set the color.</param>
        /// <param name="color">The color.</param>
        public void SetColor(Pad pad, Color color)
        {
            Message message = new Message(MessageCommand.Color);
            message.AddPayload(color);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Sets colors on all the pad at the same time immediatly.
        /// </summary>
        /// <param name="padCenter">The center Pad's color.</param>
        /// <param name="padLeft">The left Pad's color.</param>
        /// <param name="padRight">The right Pad's color.</param>
        public void SetColorAllPads(Color padCenter, Color padLeft, Color padRight)
        {
            Message message = new Message(MessageCommand.ColorAll);
            message.AddPayload(true, padCenter, true, padLeft, true, padRight);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Switches off colors on all the pad at the same time immediatly.
        /// </summary>
        public void SwithOffAllPads()
        {
            Message message = new Message(MessageCommand.ColorAll);
            message.AddPayload(false, Color.Black, false, Color.Black, false, Color.Black);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Flashes a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to flash.</param>
        /// <param name="tickOn">The time to to stay on. The higher, the longer.</param>
        /// <param name="tickOff">The time to stay off. The higher, the longer.</param>
        /// <param name="tickCount">The number of pulses, 0xFF is forever.</param>
        /// <param name="color"></param>
        public void Flash(Pad pad, byte tickOn, byte tickOff, byte tickCount, Color color)
        {
            Message message = new Message(MessageCommand.Flash);
            message.AddPayload(pad, tickOn, tickOff, tickCount, color);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Fades a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to fade.</param>
        /// <param name="tickTime">>The time to to fade. The higher, the longer.</param>
        /// <param name="tickCount">The fading behavior.</param>
        /// <param name="oldColor">The old color to fade from.</param>
        /// <param name="newColor">The new color to fade to.</param>
        /// <returns></returns>
        public void Fade(Pad pad, byte tickTime, ColorPulse tickCount, Color oldColor, Color newColor = default)
        {
            Message message = new Message(MessageCommand.Fade);
            message.AddPayload(pad, tickTime, (byte)tickCount, oldColor, newColor);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Fades a random color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to fade.</param>
        /// <param name="tickTime">The time to to fade. The higher, the longer.</param>
        /// <param name="tickCount">The fading behavior.</param>
        public void FadeRandom(Pad pad, byte tickTime, ColorPulse tickCount)
        {
            Message message = new Message(MessageCommand.FadeRandom);
            message.AddPayload(pad, tickTime, (byte)tickCount);
            IncreaseMessageId();
            SendMessage(message);
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messageId">The message ID, leave to 0 to use the internal message count.</param>
        /// <returns>The message ID for the request.</returns>
        public byte SendMessage(Message message, byte messageId = 0)
        {
            byte msgId = _messageId;
            var bytes = message.GetBytes(messageId == 0 ? _messageId++ : messageId);
            _endpointWriter.Write(bytes, ReadWriteTimeout, out int numBytes);
            // Assume it's awake if we send 32 bytes properly
            Debug.WriteLine($"SND: {BitConverter.ToString(bytes)}");
            return msgId;
        }

        private void ReadThread(object? obj)
        {
            // Read is always 32 bytes
            var readBuffer = new byte[32];
            int bytesRead;

            while (!_cancelThread.IsCancellationRequested)
            {
                try
                {
                    _endpointReader.Read(readBuffer, ReadWriteTimeout, out bytesRead);
                    Debug.WriteLine($"REC: {BitConverter.ToString(readBuffer)}");
                    if (bytesRead == 32)
                    {
                        var message = Message.CreateFromBuffer(readBuffer);
                        if (message.MessageType == MessageType.Event)
                        {
                            // In the case of an event the Message Type event, all is in the payload
                            byte pad = (byte)message.Payload[2];
                            if ((pad < 1) || (pad > 3))
                            {
                                // Not a valid message
                                continue;
                            }

                            bool present = message.Payload[3] == 0;
                            if (present)
                            {
                                _legoTag[pad - 1].Present = present;

                                byte[] uuid = new byte[7];
                                Array.Copy(message.Payload, 4, uuid, 0, uuid.Length);
                                _legoTag[pad - 1] = new LegoTagEventArgs((Pad)pad, present, uuid, null);
                                // Ask for more wuth the read command for 0x24
                                var msgToSend = new Message(MessageCommand.Read);
                                msgToSend.AddPayload(pad, 0x24);
                                _tagMessageId[pad - 1] = SendMessage(msgToSend);
                            }
                            else
                            {
                                _legoTag[pad - 1].Present = present;
                                LegoTagEvent?.Invoke(this, new LegoTagEventArgs(_legoTag[pad - 1]));
                            }
                        }
                        else if (message.MessageType == MessageType.Normal)
                        {
                            if (message.MessageCommand == MessageCommand.Read)
                            {
                                // We should check the message ID to find the tag
                                int pad = FindPadFromMdssageId(message.MessageId);

                                if (pad == -1)
                                {
                                    continue;
                                }

                                // We should have our 0x24
                                if (LegoTag.IsVehicle(message.Payload.AsSpan(8, 4).ToArray()))
                                {
                                    var vecId = LegoTag.GetVehiculeId(message.Payload.AsSpan(0, 4).ToArray());
                                    var vec = Vehicle.Vehicles.FirstOrDefault(m => m.Id == vecId);
                                    _legoTag[pad].LegoTag = vec;
                                    LegoTagEvent?.Invoke(this, new LegoTagEventArgs(_legoTag[pad]));
                                }
                                else
                                {
                                    // Ask for more
                                    var msgToSend = new Message(MessageCommand.Model);
                                    msgToSend.AddPayload(_legoTag[pad].Pad, _legoTag[pad].CardUid);
                                    _tagMessageId[pad] = SendMessage(msgToSend);
                                }
                            }
                            else if (message.MessageCommand == MessageCommand.Model)
                            {
                                // We should check the message ID to find the tag
                                int pad = FindPadFromMdssageId(message.MessageId);

                                if (pad == -1)
                                {
                                    continue;
                                }

                                var carId = LegoTag.GetCharacterId(_legoTag[pad].CardUid, message.Payload);
                                var car = Character.Characters.FirstOrDefault(m => m.Id == carId);
                                _legoTag[pad].LegoTag = car;
                                LegoTagEvent?.Invoke(this, new LegoTagEventArgs(_legoTag[pad]));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // We just don't do anything
                }
            }
        }

        private int FindPadFromMdssageId(byte messageId)
        {
            // We should check the message ID to find the tag
            int pad = -1;
            for (int i = 0; i < 3; i++)
            {
                if (_tagMessageId[i] == messageId)
                {
                    pad = i;
                    break;
                }
            }

            return pad;
        }

        private void IncreaseMessageId()
        {
            _messageId = (byte)(_messageId == 255 ? 1 : ++_messageId);
        }

        public void Dispose()
        {
            _cancelThread.Cancel();
            // Make sure the thread is stopped
            _readThread?.Join(); ;
            _portal.Close();
            _portal.Dispose();
        }
    }
}