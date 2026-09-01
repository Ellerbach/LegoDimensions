// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;
using LegoDimensions.Tag;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LegoDimensions
{
    /// <summary>
    /// Instance of a Lego Dimensions Portal.
    /// </summary>
    public class LegoPortal : IDisposable, ILegoPortal
    {
        // Constants
        private const int ProductId = 0x0241;
        private const int XboxOneProductId = 0x0141;
        private const int VendorId = 0x0E6F;
        private const int Xbox360VendorId = 0x24C6;
        private const int Xbox360ProductId = 0xFA01;
        private const int ReadWriteTimeout = 1000;
        private const int ReceiveTimeout = 2000;
        private const int XboxWakeProbeTimeout = 250;
        // Shorter than ReadWriteTimeout so a write waiting on _xbox360IoGate never has to wait
        // long for the current read attempt to give up the gate.
        private const int Xbox360ReadTimeout = 50;

        // This needs to be static to keep the context otherwise, the app will close it
        private static UsbContext? context;

        // Class variables
        private IUsbDevice _portal;
        private UsbEndpointReader _endpointReader;
        private UsbEndpointWriter _endpointWriter;
        private byte _messageId;
        private Thread _readThread;
        private CancellationTokenSource _cancelThread;
        private List<PresentTag> _presentTags = new List<PresentTag>();
        private bool _nfcEnabled = true;
        private readonly bool _isXboxPortal;
        private readonly bool _isXbox360Portal;
        private readonly object _writeLock = new object();
        private readonly object _commandLock = new object();
        // Full-duration mutual exclusion between the background read thread and writes: a background
        // thread reading concurrently with a write on another thread silently loses replies on this
        // device (confirmed via XboxPortalProbe's hybrid-async-wake-360 test). Only used for Xbox 360;
        // other portal types are unaffected and left as-is.
        private readonly SemaphoreSlim _xbox360IoGate = new SemaphoreSlim(1, 1);
        // SemaphoreSlim doesn't guarantee fairness: the read loop releasing and immediately
        // re-acquiring _xbox360IoGate in a tight cycle can starve a write waiting on the same
        // gate for many seconds. This counter lets the read loop notice a pending write and back
        // off briefly so the writer reliably gets a turn instead of losing the race repeatedly.
        private int _xbox360PendingWriters;
        // Only one tracked Read/GetColor/etc. request-reply cycle at a time on Xbox 360: sending a
        // second one before the first's reply arrives appears to corrupt/drop one of them.
        private readonly object _xbox360CommandLock = new object();
        private byte _gipSequence = 1;

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
            context ??= new UsbContext();
#if DEBUG
            context.SetDebugLevel(LogLevel.Info);
#else
            context.SetDebugLevel(LogLevel.Error);
#endif
            //Get a list of all connected devices
            var usbDeviceCollection = context.List();

            //Narrow down the device by vendor and pid
            var selectedDevice = usbDeviceCollection.Where(d =>
                (d.VendorId == VendorId && (d.ProductId == ProductId || d.ProductId == XboxOneProductId)) ||
                (d.VendorId == Xbox360VendorId && d.ProductId == Xbox360ProductId));

            return selectedDevice.ToArray();
        }

        /// <inheritdoc/>
        public event EventHandler<LegoTagEventArgs>? LegoTagEvent;

        /// <summary>
        /// Gets the list of present tags.
        /// </summary>
        public IEnumerable<PresentTag> PresentTags => _presentTags;

        /// <inheritdoc/>
        public bool NfcEnabled
        {
            get => _nfcEnabled;
            set
            {
                _nfcEnabled = value;
                var message = new Message(MessageCommand.ConfigActive);
                message.AddPayload(_nfcEnabled);
                SendMessage(message);
            }
        }

        /// <inheritdoc/>
        public bool GetTagDetails { get; set; } = true;

        /// <summary>
        /// Gets the ID.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets the underlying USB device.
        /// </summary>
        public IUsbDevice UsbDevice => _portal;

        /// <inheritdoc/>
        public bool IsXboxPortal => _isXboxPortal;

        /// <inheritdoc/>
        public bool IsXbox360Portal => _isXbox360Portal;

        /// <summary>
        /// Gets the opaque device-specific bytes returned by the wake command.
        /// </summary>
        public byte[] SerialNumber { get; internal set; } = [];

        /// <summary>
        /// Creates a new instance of a Lego Dimensions Portal.
        /// </summary>
        /// <param name="device">A valid Lego Dimensions instance.</param>
        /// <param name="id">An ID for this device, can be handy if you manage multiple ones.</param>
        public LegoPortal(IUsbDevice device, int id = 0)
        {
            _portal = device;
            _isXboxPortal = device.ProductId == XboxOneProductId;
            _isXbox360Portal = device.VendorId == Xbox360VendorId && device.ProductId == Xbox360ProductId;
            Id = id;
            //Open the device
            _portal.Open();

            // Non Windows OS need to detach the kernel driver
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Imports.SetAutoDetachKernelDriver(_portal.DeviceHandle, 1);
            }

            if (_isXboxPortal)
            {
                try
                {
                    _portal.SetConfiguration(_portal.Configs[0].ConfigurationValue);
                }
                catch (UsbException)
                {
                    // Continue when the active configuration cannot be selected again.
                }
            }
            else if (_isXbox360Portal && !OperatingSystem.IsWindows())
            {
                // toypad.py's init() skips reset()/set_configuration() entirely on win32; only call it elsewhere.
                try
                {
                    _portal.SetConfiguration(_portal.Configs[0].ConfigurationValue);
                }
                catch (UsbException)
                {
                    // Continue when the active configuration cannot be selected again.
                }
            }

            // Only interface 0 (the main gateway) is ever claimed; the Xbox 360's XSM3 security
            // interface (3) is intentionally never bound - the toypad replies to LEGO commands
            // without authentication, so no security-endpoint binding is needed.
            _portal.ClaimInterface(_portal.Configs[0].Interfaces[0].Number);

            //Open up the endpoints
            _endpointWriter = _portal.OpenEndpointWriter(WriteEndpointID.Ep01);
            _endpointReader = _portal.OpenEndpointReader(ReadEndpointID.Ep01);

            if (!_isXboxPortal && !_isXbox360Portal)
            {
                // Read the first 32 bytes
                var readBuffer = new byte[32];
                _endpointReader.Read(readBuffer, ReadWriteTimeout, out _);
            }

            // Start the read thread
            _cancelThread = new CancellationTokenSource();
            _readThread = new Thread(ReadThread)
            {
                // Must not keep the process alive if construction fails before Dispose can be called.
                IsBackground = true
            };
            _readThread.Start();

            try
            {
                // WakeUp the portal
                WakeUp();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public void WakeUp()
        {
            Message message = new Message(MessageCommand.Wake);
            message.AddPayload("(c) LEGO 2014");
            var getSerial = new ManualResetEvent(false);
            SerialNumber = new byte[0];

            if (_isXbox360Portal)
            {
                // toypad.py's start() hardcodes message_id=0 for wake; SendMessage/IncreaseMessageId
                // treat 0 as "auto-assign", so this bypasses that path to send the literal ID 0.
                var xbox360CommandId = new CommandId(0, MessageCommand.Wake, getSerial);
                lock (_commandLock)
                {
                    _commandId.Add(xbox360CommandId);
                }

                try
                {
                    WriteUsbPacket(Xbox360Transport.WrapLegoFrame(message.GetBytes(0)));
                }
                catch
                {
                    RemoveCommand(xbox360CommandId);
                    throw;
                }

                getSerial.WaitOne(ReceiveTimeout, true);
                if (xbox360CommandId.Result != null)
                {
                    SerialNumber = (byte[])xbox360CommandId.Result;
                }

                RemoveCommand(xbox360CommandId);
                return;
            }

            _messageId = 0;
            var commandId = SendTrackedMessage(message, MessageCommand.Wake, getSerial);

            var wakeReceived = _isXboxPortal && getSerial.WaitOne(XboxWakeProbeTimeout, true);
            if (_isXboxPortal && !wakeReceived)
            {
                WriteUsbPacket(XboxGipTransport.CreatePacket(
                    XboxGipTransport.AuthenticateCommand,
                    0x20,
                    IncreaseGipSequence(),
                    [0x01, 0x00]));
            }

            if (!wakeReceived)
            {
                getSerial.WaitOne(ReceiveTimeout, true);
            }

            if (commandId.Result != null)
            {
                SerialNumber = (byte[])commandId.Result;
            }

            RemoveCommand(commandId);
            // TODO: investigate seeding
            //message = new Message(MessageCommand.Seed);
            //message.AddPayload(new byte[] { 0xaa, 0x6F, 0xC8, 0xCD, 0x21, 0x1E, 0xF8, 0xCE });
            //SendMessage(message);
        }

        /// <inheritdoc/>
        public void SetColor(Pad pad, Color color)
        {
            Message message = new Message(MessageCommand.Color);
            message.AddPayload(pad, color);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public Color GetColor(Pad pad)
        {
            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.GetColor);
                message.AddPayload(pad);
                var getColor = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.GetColor, getColor);
                // In case we won't get any color, use the default black one
                Color padColor = Color.Black;
                // Wait maximum 2 seconds
                getColor.WaitOne(ReceiveTimeout, true);

                if (commandId.Result != null)
                {
                    padColor = (Color)commandId.Result;
                }

                RemoveCommand(commandId);

                return padColor;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        /// <inheritdoc/>
        public void SetColorAll(Color padCenter, Color padLeft, Color padRight)
        {
            Message message = new Message(MessageCommand.ColorAll);
            message.AddPayload(true, padCenter, true, padLeft, true, padRight);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public void SwitchOffAll()
        {
            SetColor(Pad.All, Color.Black);
        }

        /// <inheritdoc/>
        public void Flash(Pad pad, FlashPad flashPad)
        {
            Message message = new Message(MessageCommand.Flash);
            message.AddPayload(pad, flashPad.TickOn, flashPad.TickOff, flashPad.TickCount, flashPad.Color);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public void FlashAll(FlashPad flashPadCenter, FlashPad flashPadLeft, FlashPad flashPadRight)
        {
            Message message = new Message(MessageCommand.FlashAll);
            message.AddPayload(flashPadCenter.Enabled, flashPadCenter.TickOn, flashPadCenter.TickOff, flashPadCenter.TickCount, flashPadCenter.Color);
            message.AddPayload(flashPadLeft.Enabled, flashPadLeft.TickOn, flashPadLeft.TickOff, flashPadLeft.TickCount, flashPadLeft.Color);
            message.AddPayload(flashPadRight.Enabled, flashPadRight.TickOn, flashPadRight.TickOff, flashPadRight.TickCount, flashPadRight.Color);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public void Fade(Pad pad, FadePad fadePad)
        {
            Message message = new Message(MessageCommand.Fade);
            message.AddPayload(pad, fadePad.TickTime, fadePad.TickCount, fadePad.Color);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public void FadeAll(FadePad fadePadCenter, FadePad fadePadLeft, FadePad fadePadRight)
        {
            Message message = new Message(MessageCommand.FadeAll);
            message.AddPayload(fadePadCenter.Enabled, fadePadCenter.TickTime, fadePadCenter.TickCount, fadePadCenter.Color);
            message.AddPayload(fadePadLeft.Enabled, fadePadLeft.TickTime, fadePadLeft.TickCount, fadePadLeft.Color);
            message.AddPayload(fadePadRight.Enabled, fadePadRight.TickTime, fadePadRight.TickCount, fadePadRight.Color);
            SendMessage(message);
        }

        /// <inheritdoc/>
        public void FadeRandom(Pad pad, byte tickTime, byte tickCount)
        {
            Message message = new Message(MessageCommand.FadeRandom);
            message.AddPayload(pad, tickTime, tickCount);
            SendMessage(message);
        }

        /// <summary>
        /// Read 16 bytes from a tag.
        /// </summary>
        /// <param name="index">The tag index to read.</param>
        /// <param name="page">The page to read.</param>
        /// <returns>A byte array of 16 bytes in case of success.</returns>
        public byte[] ReadTag(byte index, byte page)
        {
            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.Read);
                message.AddPayload(index, page);
                var getRead = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.Read, getRead);
                // In case we won't get any color, use the default black one
                byte[] readBytes = new byte[0];
                // Wait maximum 2 seconds
                getRead.WaitOne(ReceiveTimeout, true);

                if (commandId.Result != null)
                {
                    readBytes = (byte[])commandId.Result;
                }

                RemoveCommand(commandId);

                return readBytes;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        /// <summary>
        /// Write 4 bytes to a tag.
        /// </summary>
        /// <param name="index">The tag index to write.</param>
        /// <param name="page">The page to write.</param>
        /// <param name="bytes">An array of 4 bytes to write.</param>
        /// <returns>True if success.</returns>
        public bool WriteTag(byte index, byte page, byte[] bytes)
        {
            if (bytes.Length != 4)
            {
                throw new ArgumentException("Write to card must be 4 bytes.");
            }

            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.Write);
                message.AddPayload(index, page, bytes);
                var getWrite = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.Write, getWrite);
                // In case we won't get any color, use the default black one
                bool success = false;
                // Wait maximum 2 seconds
                getWrite.WaitOne(ReceiveTimeout, true);

                if (commandId.Result != null)
                {
                    success = (bool)commandId.Result;
                }

                RemoveCommand(commandId);

                return success;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        /// <summary>
        /// Read 8 bytes from a tag at page 0x24 and 0x25.
        /// </summary>
        /// <param name="encryoptedIndex">The tag index to read encrypted on 8 bytes.</param>
        /// <returns>A byte array of 8 bytes in case of success.</returns>
        public byte[] GetTagInformation(byte[] encryoptedIndex)
        {
            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.Model);
                message.AddPayload(encryoptedIndex);
                var getRead = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.Model, getRead);
                // In case we won't get any color, use the default black one
                byte[] readBytes = new byte[0];
                // Wait maximum 2 seconds
                getRead.WaitOne(ReceiveTimeout, true);

                if (commandId.Result != null)
                {
                    readBytes = (byte[])commandId.Result;
                }

                RemoveCommand(commandId);

                return readBytes;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        /// <summary>
        /// Get a challenge from the portal.
        /// </summary>
        /// <returns>A byte array of 8 bytes in case of success.</returns>
        public byte[] GetChallenge()
        {
            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.Challenge);
                var getChallenge = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.Challenge, getChallenge);
                // In case we won't get any color, use the default black one
                byte[] readBytes = new byte[0];
                // Wait maximum 2 seconds
                getChallenge.WaitOne(ReceiveTimeout, true);

                if (commandId.Result != null)
                {
                    readBytes = (byte[])commandId.Result;
                }

                RemoveCommand(commandId);

                return readBytes;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        public IEnumerable<PresentTag> ListTags()
        {
            if (_isXbox360Portal)
            {
                Monitor.Enter(_xbox360CommandLock);
            }

            try
            {
                Message message = new Message(MessageCommand.TagList);
                var getTagList = new ManualResetEvent(false);
                var commandId = SendTrackedMessage(message, MessageCommand.TagList, getTagList);
                // Bounded like the other tracked commands: an unbounded wait here would spin forever
                // if the portal never replies, permanently blocking every other Xbox 360 command
                // waiting on _xbox360CommandLock.
                getTagList.WaitOne(ReceiveTimeout, true);

                // We don't do anything as we manage the result globally
                RemoveCommand(commandId);

                return _presentTags;
            }
            finally
            {
                if (_isXbox360Portal)
                {
                    Monitor.Exit(_xbox360CommandLock);
                }
            }
        }

        /// <summary>
        /// Sets the tag password behavior.
        /// </summary>
        /// <param name="password">The desired state, Automatic is the default value.</param>
        /// <param name="index">The tag index.</param>
        /// <param name="newPassword">The new 4 bytes password if any.</param>
        public void SetTagPassword(PortalPassword password, byte index, byte[]? newPassword = null)
        {
            if (password == PortalPassword.Custom)
            {
                if (newPassword != null && newPassword.Length != 4)
                {
                    throw new ArgumentException("New password must be 4 bytes");
                }
            }

            Message message = new Message(MessageCommand.ConfigPassword);
            message.AddPayload((byte)password, index, newPassword == null ? new byte[4] : newPassword);
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
            var id = messageId == 0 ? IncreaseMessageId() : messageId;
            var legoMessage = message.GetBytes(id);
            var bytes = _isXboxPortal
                ? XboxGipTransport.CreatePacket(XboxGipTransport.LegoGatewayCommand, 0x00, IncreaseGipSequence(), legoMessage)
                : _isXbox360Portal
                    ? Xbox360Transport.WrapLegoFrame(legoMessage)
                    : legoMessage;
            WriteUsbPacket(bytes);
            return id;
        }

        private void ReadThread(object? obj)
        {
            var readBuffer = new byte[_isXboxPortal ? 1024 : 32];
            int bytesRead;

            while (!_cancelThread.IsCancellationRequested)
            {
                try
                {
                    Error error;
                    if (_isXbox360Portal)
                    {
                        if (Volatile.Read(ref _xbox360PendingWriters) > 0)
                        {
                            // Back off instead of immediately re-racing for the gate, so the waiting
                            // write gets a fair chance instead of being starved by this loop.
                            Thread.Sleep(5);
                        }

                        // Held for the full duration of the call so a concurrent write (from another
                        // thread) can never overlap with this read on the same device handle.
                        _xbox360IoGate.Wait(_cancelThread.Token);
                        try
                        {
                            error = _endpointReader.Read(readBuffer, Xbox360ReadTimeout, out bytesRead);
                        }
                        finally
                        {
                            _xbox360IoGate.Release();
                        }
                    }
                    else
                    {
                        error = _endpointReader.Read(readBuffer, ReadWriteTimeout, out bytesRead);
                    }

                    if (error == Error.Timeout)
                    {
                        continue;
                    }

                    if (error != Error.Success)
                    {
                        Debug.WriteLine($"USB read error: {error}");
                        if (error == Error.NoDevice || error == Error.NotFound)
                        {
                            break;
                        }

                        continue;
                    }

                    var bufferedBytes = !_isXboxPortal && bytesRead == readBuffer.Length + 1
                        ? readBuffer.Length
                        : bytesRead;
                    if (bufferedBytes > 0)
                    {
                        Debug.WriteLine($"REC: {BitConverter.ToString(readBuffer, 0, bufferedBytes)}");
                    }

                    foreach (var legoMessage in ExtractLegoMessages(readBuffer, bufferedBytes))
                    {
                        var message = Message.CreateFromBuffer(legoMessage, MessageSource.Portal);
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
                                    _presentTags.Add(new PresentTag((Pad)pad, tadType, padIndex, uuid));
                                    legoTag = new PadTag() { Pad = (Pad)pad, TagIndex = padIndex, Present = present, CardUid = uuid, TagType = tadType };
                                    _padTag.Add(legoTag);

                                    if (GetTagDetails)
                                    {
                                        // Ask for more wuth the read command for 0x24
                                        var msgToSend = new Message(MessageCommand.Read);
                                        msgToSend.AddPayload(padIndex, (byte)0x24);
                                        // On Xbox 360, skip this if an explicit call (ReadTag/GetColor/etc.) is
                                        // currently in flight on another thread: blocking here would deadlock
                                        // this read thread against that call's own wait for its reply, and
                                        // sending both at once appears to corrupt/drop one of the replies.
                                        var autoReadLockTaken = !_isXbox360Portal;
                                        if (_isXbox360Portal)
                                        {
                                            Monitor.TryEnter(_xbox360CommandLock, 0, ref autoReadLockTaken);
                                        }

                                        if (autoReadLockTaken)
                                        {
                                            try
                                            {
                                                legoTag.LastMessageId = SendTrackedMessage(msgToSend, MessageCommand.Read, null).MessageId;
                                            }
                                            finally
                                            {
                                                if (_isXbox360Portal)
                                                {
                                                    Monitor.Exit(_xbox360CommandLock);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Directly raise the event
                                        LegoTagEvent?.Invoke(this, new LegoTagEventArgs(legoTag));
                                    }
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
                            CommandId? cmdId;
                            lock (_commandLock)
                            {
                                cmdId = _commandId.FirstOrDefault(m => m.MessageId == message.MessageId);
                            }
                            if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Read)
                            {
                                // In this case the request is coming from the event
                                if (cmdId.ManualResetEvent == null)
                                {
                                    var legoTag = _padTag.FirstOrDefault(m => m.LastMessageId == message.MessageId);
                                    if (legoTag == null)
                                    {
                                        continue;
                                    }

                                    // We should have our 0x24
                                    if (LegoTag.IsVehicle(message.Payload.AsSpan(9, 4).ToArray()))
                                    {
                                        var vecId = LegoTag.GetVehicleId(message.Payload.AsSpan(1, 4).ToArray());
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
                                    RemoveCommand(cmdId);
                                }
                                else
                                {
                                    // This case is a normal read and we will set the buffer
                                    if (message.Payload[0] == 0)
                                    {
                                        // if no error, we set the result
                                        cmdId.Result = message.Payload[1..];
                                    }

                                    cmdId.ManualResetEvent.Set();
                                }
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.GetColor)
                            {
                                cmdId.Result = Color.FromArgb(message.Payload[0], message.Payload[1], message.Payload[2]);
                                cmdId.ManualResetEvent?.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Model)
                            {
                                if (message.Payload[0] == 0)
                                {
                                    // if no error, we set the result
                                    cmdId.Result = message.Payload[1..];
                                }

                                cmdId.ManualResetEvent?.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Write)
                            {
                                cmdId.Result = message.Payload[0] == 0;
                                cmdId.ManualResetEvent?.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Wake)
                            {
                                cmdId.Result = message.Payload[10..];
                                cmdId.ManualResetEvent?.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.Challenge)
                            {
                                cmdId.Result = message.Payload;
                                cmdId.ManualResetEvent?.Set();
                            }
                            else if (message.MessageCommand == MessageCommand.None && cmdId != null && cmdId.MessageCommand == MessageCommand.TagList)
                            {
                                _presentTags.Clear();
                                for (int i = 0; i < message.Payload.Length / 2; i++)
                                {
                                    var index = (byte)(message.Payload[i * 2] & 0xF);
                                    var uid = _padTag.FirstOrDefault(x => x.TagIndex == index)?.CardUid ?? Array.Empty<byte>();
                                    PresentTag presentTag = new PresentTag((Pad)(message.Payload[i * 2] >> 4), (TagType)message.Payload[i * 2 + 1], index, uid);
                                    _presentTags.Add(presentTag);
                                }

                                cmdId.ManualResetEvent?.Set();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Excption: {ex}");
                }
            }
        }

        private byte IncreaseMessageId()
        {
            _messageId = (byte)(_messageId == 255 ? 1 : ++_messageId);
            return _messageId;
        }

        private CommandId SendTrackedMessage(Message message, MessageCommand command, ManualResetEvent? resetEvent)
        {
            var commandId = new CommandId(IncreaseMessageId(), command, resetEvent);
            lock (_commandLock)
            {
                _commandId.Add(commandId);
            }

            try
            {
                SendMessage(message, commandId.MessageId);
                return commandId;
            }
            catch
            {
                RemoveCommand(commandId);
                throw;
            }
        }

        private void RemoveCommand(CommandId commandId)
        {
            lock (_commandLock)
            {
                _commandId.Remove(commandId);
            }
        }

        private byte IncreaseGipSequence()
        {
            var sequence = _gipSequence++;
            if (_gipSequence == 0)
            {
                _gipSequence = 1;
            }

            return sequence;
        }

        private IEnumerable<byte[]> ExtractLegoMessages(byte[] buffer, int bytesRead)
        {
            if (_isXbox360Portal)
            {
                if (Xbox360Transport.TryUnwrapLegoFrame(buffer.AsSpan(0, bytesRead), out var standardFrame))
                {
                    yield return standardFrame;
                }

                yield break;
            }

            if (!_isXboxPortal)
            {
                if (bytesRead == 32)
                {
                    yield return buffer[..32];
                }

                yield break;
            }

            var offset = 0;
            while (offset < bytesRead && XboxGipTransport.TryGetPacket(buffer.AsSpan(offset, bytesRead - offset), out var packetLength, out var command, out var payload))
            {
                if (command == XboxGipTransport.LegoGatewayCommand && payload.Length == 32)
                {
                    yield return payload.ToArray();
                }

                offset += packetLength;
            }
        }

        private void WriteUsbPacket(byte[] bytes)
        {
            lock (_writeLock)
            {
                Error error;
                int bytesWritten;
                if (_isXbox360Portal)
                {
                    Interlocked.Increment(ref _xbox360PendingWriters);
                    try
                    {
                        _xbox360IoGate.Wait();
                        try
                        {
                            error = _endpointWriter.Write(bytes, ReadWriteTimeout, out bytesWritten);
                        }
                        finally
                        {
                            _xbox360IoGate.Release();
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _xbox360PendingWriters);
                    }
                }
                else
                {
                    error = _endpointWriter.Write(bytes, ReadWriteTimeout, out bytesWritten);
                }

                var complete = bytesWritten == bytes.Length ||
                    (!_isXboxPortal && !_isXbox360Portal && bytes.Length == 32 && bytesWritten == 33);
                if (error != Error.Success || !complete)
                {
                    throw new IOException($"USB endpoint write failed with {error}; reported {bytesWritten} for {bytes.Length} bytes.");
                }
            }

            Debug.WriteLine($"SND: {BitConverter.ToString(bytes)}");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_cancelThread is not null && !_cancelThread.IsCancellationRequested)
            {
                _cancelThread.Cancel();
            }

            // Make sure the thread is stopped before releasing native resources.
            _readThread?.Join(2000);

            LegoTagEvent = null;
            _presentTags?.Clear();
            _padTag?.Clear();
            _commandId?.Clear();

            if (_portal is not null)
            {
                _portal.ReleaseInterface(_portal.Configs[0].Interfaces[0].Number);
                _portal.Close();
                _portal.Dispose();
            }

            _xbox360IoGate?.Dispose();
        }
    }
}