// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Collections.Generic;
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
        private List<PadTag> _padTag = new List<PadTag>();

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
            _messageId = 0;
            SendMessage(message);
            // TODO: investigate seeding
            //message = new Message(MessageCommand.Seed);
            //message.AddPayload(new byte[] { 0xaa, 0x6F, 0xC8, 0xCD, 0x21, 0x1E, 0xF8, 0xCE });
            //SendMessage(message);
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
            SendMessage(message);
        }

        /// <summary>
        /// Switches off colors on all the pad at the same time immediatly.
        /// </summary>
        public void SwithOffAllPads()
        {
            Message message = new Message(MessageCommand.ColorAll);
            message.AddPayload(false, Color.Black, false, Color.Black, false, Color.Black);
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
            SendMessage(message);
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messageId">The message ID, leave to 0 to use the internal message count.</param>
        /// <returns>The message ID for the request.</returns>
        public byte SendMessage(Message message, byte messageId = 0)
        {            ;
            var bytes = message.GetBytes(messageId == 0 ? IncreaseMessageId() : messageId);
            _endpointWriter.Write(bytes, ReadWriteTimeout, out int numBytes);
            // Assume it's awake if we send 32 bytes properly
            Debug.WriteLine($"SND: {BitConverter.ToString(bytes)}");
            return messageId == 0 ? _messageId : messageId;
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
                        var message = Message.CreateFromBufferIncoming(readBuffer);
                        if (message.MessageType == MessageType.Event)
                        {
                            // In the case of an event the Message Type event, all is in the payload
                            byte pad = (byte)message.Payload[0];
                            if ((pad < 1) || (pad > 3))
                            {
                                // Not a valid message
                                continue;
                            }

                            bool present = message.Payload[3] == 0;
                            byte[] uuid = new byte[7];
                            Array.Copy(message.Payload, 4, uuid, 0, uuid.Length);
                            // Find the tage if existing in the list
                            var legoTag = _padTag.FirstOrDefault(m => m.CardUid.SequenceEqual(uuid));
                            byte padIndex = message.Payload[2];
                            if (present)
                            {

                                if (legoTag == null)
                                {
                                    legoTag = new PadTag() { Pad = (Pad)pad, TagIndex = padIndex, Present = present, CardUid = uuid };
                                    _padTag.Add(legoTag);

                                    // Ask for more wuth the read command for 0x24
                                    var msgToSend = new Message(MessageCommand.Read);
                                    msgToSend.AddPayload(padIndex, (byte)0x24);
                                    legoTag.LastMessageId = SendMessage(msgToSend);
                                }
                                else
                                {
                                    legoTag.Present = present;
                                }
                            }
                            else
                            {
                                if (legoTag != null)
                                {
                                    legoTag.Present = present;
                                    LegoTagEvent?.Invoke(this, new LegoTagEventArgs(legoTag));
                                    _padTag.Remove(legoTag);
                                }
                            }
                        }
                        else if (message.MessageType == MessageType.Normal)
                        {
                            // In case the paylod is 17, then we do have a response to a read command
                            if (message.MessageCommand == MessageCommand.None && message.Payload.Length == 17)
                            {
                                var legoTag = _padTag.FirstOrDefault(m => m.LastMessageId == message.MessageId);
                                if (legoTag == null)
                                {
                                    continue;
                                }

                                // We should have our 0x24
                                if (LegoTag.IsVehicle(message.Payload.AsSpan(9, 4).ToArray()))
                                {
                                    var vecId = LegoTag.GetVehiculeId(message.Payload.AsSpan(1, 4).ToArray());
                                    var vec = Vehicle.Vehicles.FirstOrDefault(m => m.Id == vecId);
                                    legoTag.LegoTag = vec;

                                }
                                else
                                {
                                    var carId = LegoTag.GetCharacterId(legoTag.CardUid, message.Payload.AsSpan(1, 8).ToArray());
                                    var car = Character.Characters.FirstOrDefault(m => m.Id == carId);
                                    legoTag.LegoTag = car;
                                }

                                LegoTagEvent?.Invoke(this, new LegoTagEventArgs(legoTag));
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

        private byte IncreaseMessageId()
        {
            _messageId = (byte)(_messageId == 255 ? 1 : ++_messageId);
            return _messageId;
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