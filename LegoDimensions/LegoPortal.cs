// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.ComponentModel.Design;
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
        private ManualResetEvent _getColor;
        private ManualResetEvent _getTagList;
        private Color _padColor;
        private List<PresentTag> _presentTags = new List<PresentTag>();

        // We do have only 3 Pads
        // This one is to store the last message ID request for details
        private List<PadTag> _padTag = new List<PadTag>();
        private List<CommandId> _commandId = new List<CommandId>();

        /// <summary>
        /// Gets the first of default Lego Dimensions Portal.
        /// </summary>
        /// <returns>A Lego Dimensions Portal</returns>
        public static LegoPortal GetFirstPortal()
        {
            var portals = GetPortals();

            if (portals.Length == 0)
            {
                throw new Exception("No Lego Dimensions Portal found.");
            }

            return new LegoPortal(portals[0]);
        }

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

        /// <summary>
        /// Event when a tag is added or removed
        /// </summary>
        public event EventHandler<LegoTagEventArgs>? LegoTagEvent;

        /// <summary>
        /// Gets the list of present tags.
        /// </summary>
        public IEnumerable<PresentTag> PresentTags => _presentTags;

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
            message.AddPayload(pad, color);
            SendMessage(message);
        }

        /// <summary>
        /// Gets the color of a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad to get the color.</param>
        /// <returns></returns>
        public Color GetColor(Pad pad)
        {
            Message message = new Message(MessageCommand.GetColor);
            message.AddPayload(pad);
            _getColor = new ManualResetEvent(false);
            _commandId.Add(new CommandId(SendMessage(message), MessageCommand.GetColor));
            // In case we won't get any color, use the default black one
            _padColor = Color.Black;
            // Wait maximum 2 seconds
            while (!_getColor.WaitOne(2000))
            {
                // Wait for the color to be received
            }

            return _padColor;
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
        /// <param name="flashPad">The flash pad settings.</param>
        public void Flash(Pad pad, FlashPad flashPad)
        {
            Message message = new Message(MessageCommand.Flash);
            message.AddPayload(pad, flashPad.TickOn, flashPad.TickOff, flashPad.TickCount, flashPad.Color);
            SendMessage(message);
        }

        /// <summary>
        /// Flashes a color on a all pads.
        /// </summary>
        /// <param name="flashPadCenter">The flash pad settings for center pad.</param>
        /// <param name="flashPadLeft">The flash pad settings for pad left.</param>
        /// <param name="flashPadRight">The flash pad settings for pad right.</param>
        public void FlashAll(FlashPad flashPadCenter, FlashPad flashPadLeft, FlashPad flashPadRight)
        {
            Message message = new Message(MessageCommand.FlashAll);
            message.AddPayload(flashPadCenter.Enabled, flashPadCenter.TickOn, flashPadCenter.TickOff, flashPadCenter.TickCount, flashPadCenter.Color);
            message.AddPayload(flashPadLeft.Enabled, flashPadLeft.TickOn, flashPadLeft.TickOff, flashPadLeft.TickCount, flashPadLeft.Color);
            message.AddPayload(flashPadRight.Enabled, flashPadRight.TickOn, flashPadRight.TickOff, flashPadRight.TickCount, flashPadRight.Color);
            SendMessage(message);
        }

        /// <summary>
        /// Fades a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to fade.</param>
        /// <param name="fadePad">The fade pad settings.</param>
        public void Fade(Pad pad, FadePad fadePad)
        {
            Message message = new Message(MessageCommand.Fade);
            message.AddPayload(pad, fadePad.TickTime, fadePad.TickCount, fadePad.Color);
            SendMessage(message);
        }

        /// <summary>
        /// Fades a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to fade.</param>
        /// <param name="fadePad">The fade pad settings.</param>
        public void FadeAll(FadePad fadePadCenter, FadePad fadePadLeft, FadePad fadePadRight)
        {
            Message message = new Message(MessageCommand.FadeAll);
            message.AddPayload(fadePadCenter.Enabled, fadePadCenter.TickTime, fadePadCenter.TickCount, fadePadCenter.Color);
            message.AddPayload(fadePadLeft.Enabled, fadePadLeft.TickTime, fadePadLeft.TickCount, fadePadLeft.Color);
            message.AddPayload(fadePadRight.Enabled, fadePadRight.TickTime, fadePadRight.TickCount, fadePadRight.Color);
            SendMessage(message);
        }

        /// <summary>
        /// Fades a random color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to fade.</param>
        /// <param name="tickTime">The time to to fade. The higher, the longer.</param>
        /// <param name="tickCount">>The tick count. Even will stop on old color, odd on the new one.</param>
        public void FadeRandom(Pad pad, byte tickTime, byte tickCount)
        {
            Message message = new Message(MessageCommand.FadeRandom);
            message.AddPayload(pad, tickTime, tickCount);
            SendMessage(message);
        }

        public IEnumerable<PresentTag> ListTags()
        {
            Message message = new Message(MessageCommand.TagList);
            _getTagList = new ManualResetEvent(false);
            _commandId.Add(new CommandId(SendMessage(message), MessageCommand.TagList));
            while (!_getTagList.WaitOne(2000))
            {
                // Wait for the taglist to be received
            }

            return _presentTags;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messageId">The message ID, leave to 0 to use the internal message count.</param>
        /// <returns>The message ID for the request.</returns>
        public byte SendMessage(Message message, byte messageId = 0)
        {
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
                    if (bytesRead > 0)
                    {
                        Debug.WriteLine($"REC: {BitConverter.ToString(readBuffer)}");
                    }

                    if (bytesRead == 32)
                    {
                        var message = Message.CreateFromBuffer(readBuffer, MessageSource.Portal);
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
                            var tadType = (TagType)message.Payload[1];
                            byte[] uuid = new byte[7];
                            Array.Copy(message.Payload, 4, uuid, 0, uuid.Length);
                            // Find the tage if existing in the list
                            var legoTag = _padTag.FirstOrDefault(m => m.CardUid.SequenceEqual(uuid));
                            byte padIndex = message.Payload[2];
                            if (present)
                            {

                                if (legoTag == null)
                                {
                                    _presentTags.Add(new PresentTag((Pad)pad, tadType, padIndex));
                                    legoTag = new PadTag() { Pad = (Pad)pad, TagIndex = padIndex, Present = present, CardUid = uuid, TagType = tadType };
                                    _padTag.Add(legoTag);

                                    // Ask for more wuth the read command for 0x24
                                    var msgToSend = new Message(MessageCommand.Read);
                                    msgToSend.AddPayload(padIndex, (byte)0x24);
                                    legoTag.LastMessageId = SendMessage(msgToSend);
                                    _commandId.Add(new CommandId(legoTag.LastMessageId, MessageCommand.Read));
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
                                    var presentTag = _presentTags.FirstOrDefault(m => m.Pad == legoTag.Pad && m.Index == legoTag.TagIndex);
                                    if (presentTag != null)
                                    {
                                        _presentTags.Remove(presentTag);
                                    }

                                    _padTag.Remove(legoTag);
                                }
                            }
                        }
                        else if (message.MessageType == MessageType.Normal)
                        {
                            // In case the paylod is 17, then we do have a response to a read command
                            var cmdId = _commandId.Where(m => m.MessageId == _messageId).FirstOrDefault();
                            if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Read)
                            {
                                _commandId.Remove(cmdId);
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
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Color)
                            {
                                _commandId.Remove(cmdId);
                                _padColor = Color.FromArgb(message.Payload[0], message.Payload[1], message.Payload[2]);
                                _getColor.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.TagList)
                            {
                                _commandId.Remove(cmdId);
                                _presentTags.Clear();
                                for (int i = 0; i < message.Payload.Length / 2; i++)
                                {
                                    PresentTag presentTag = new PresentTag((Pad)(message.Payload[i * 2] >> 4), (TagType)message.Payload[i * 2 + 1], (byte)(message.Payload[i * 2] & 0xF));
                                    _presentTags.Add(presentTag);
                                }

                                _getTagList.Set();
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
            _readThread?.Join();
            _portal.ReleaseInterface(_portal.Configs[0].Interfaces[0].Number);
            _portal.Close();
            _portal.Dispose();
        }
    }
}