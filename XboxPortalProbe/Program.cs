using LegoDimensions.Portal;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

const int VendorId = 0x0E6F;
const int XboxProductId = 0x0141;
const int Xbox360VendorId = 0x24C6;
const int Xbox360ProductId = 0xFA01;
const int TransferTimeout = 250;
const int WakeProbeTimeout = 250;

UsbContext context;
try
{
    context = new UsbContext();
}
catch (DllNotFoundException)
{
    Console.Error.WriteLine("The native libusb-1.0 library could not be loaded.");
    Console.Error.WriteLine("On Windows, run .\\XboxPortalProbe\\tools\\update-libusb.ps1 to fetch it into XboxPortalProbe\\native.");
    Console.Error.WriteLine("On Linux/macOS, install libusb through your system package manager (e.g. apt install libusb-1.0-0, brew install libusb).");
    return 2;
}

Console.WriteLine($"Native libusb version: {NativeLibUsb.GetVersion()}");

using var disposableContext = context;
if (args.Contains("list-usb", StringComparer.OrdinalIgnoreCase))
{
    foreach (var usbDevice in context.List())
    {
        string productInfo;
        try
        {
            usbDevice.Open();
            productInfo = $"{usbDevice.Info.Manufacturer} {usbDevice.Info.Product}".Trim();
            usbDevice.Close();
        }
        catch (UsbException)
        {
            productInfo = "(could not open to read strings)";
        }

        Console.WriteLine($"{usbDevice.VendorId:X4}:{usbDevice.ProductId:X4}  {productInfo}");
    }

    return 0;
}

if (args.Length == 3 && args[0].Equals("describe", StringComparison.OrdinalIgnoreCase))
{
    if (!ushort.TryParse(args[1], System.Globalization.NumberStyles.HexNumber, null, out var describeVendorId) ||
        !ushort.TryParse(args[2], System.Globalization.NumberStyles.HexNumber, null, out var describeProductId))
    {
        Console.Error.WriteLine("Usage: describe <vid-hex> <pid-hex>");
        return 1;
    }

    return DescribeDevice(context, describeVendorId, describeProductId);
}

if (args.Length == 3 && args[0].Equals("probe", StringComparison.OrdinalIgnoreCase))
{
    if (!ushort.TryParse(args[1], System.Globalization.NumberStyles.HexNumber, null, out var probeVendorId) ||
        !ushort.TryParse(args[2], System.Globalization.NumberStyles.HexNumber, null, out var probeProductId))
    {
        Console.Error.WriteLine("Usage: probe <vid-hex> <pid-hex>");
        return 1;
    }

    return ProbeXbox360(context, probeVendorId, probeProductId);
}

if (args.Contains("probe-360", StringComparer.OrdinalIgnoreCase))
{
    return ProbeXbox360(context, Xbox360VendorId, Xbox360ProductId);
}

if (args.Contains("describe-360", StringComparer.OrdinalIgnoreCase))
{
    return DescribeDevice(context, Xbox360VendorId, Xbox360ProductId);
}

if (args.Contains("raw-wake-360", StringComparer.OrdinalIgnoreCase))
{
    return RunRawWake360Test();
}

if (args.Contains("hybrid-wake-360", StringComparer.OrdinalIgnoreCase))
{
    return RunHybridWake360Test(context);
}

if (args.Contains("hybrid-async-wake-360", StringComparer.OrdinalIgnoreCase))
{
    return await RunHybridAsyncWake360Test(context);
}

var connectedDevices = context.List();
var devices = connectedDevices
    .Where(device => device.VendorId == VendorId && device.ProductId == XboxProductId)
    .ToArray();

if (devices.Length == 0)
{
    if (connectedDevices.Any(device => device.VendorId == Xbox360VendorId && device.ProductId == Xbox360ProductId))
    {
        Console.WriteLine("Xbox One portal was not found; using Xbox 360 portal 24C6:FA01.");
        return ProbeXbox360(context, Xbox360VendorId, Xbox360ProductId);
    }

    Console.Error.WriteLine("Xbox portal 0E6F:0141 was not found by libusb.");
    Console.Error.WriteLine("Xbox 360 portal 24C6:FA01 was not found by libusb.");
    Console.Error.WriteLine("On Windows, its Xbox Gaming Device driver may need to be replaced with WinUSB using Zadig.");
    return 1;
}

if (devices.Length > 1)
{
    Console.WriteLine($"Found {devices.Length} Xbox portals; using the first one.");
}

var device = devices[0];
using var disposableDevice = device;
try
{
    device.Open();
}
catch (UsbException exception)
{
    Console.Error.WriteLine($"Xbox portal {VendorId:X4}:{XboxProductId:X4} was found but could not be opened: {exception.Message}");
    Console.Error.WriteLine("On Windows, replace its Xbox Gaming Device driver with WinUSB using Zadig for this test.");
    return 3;
}

var configuration = device.Configs[0];
try
{
    device.SetConfiguration(configuration.ConfigurationValue);
}
catch (UsbException exception)
{
    Console.WriteLine($"USB configuration {configuration.ConfigurationValue} could not be selected; continuing with the active configuration: {exception.Message}");
}

var interfaceNumber = configuration.Interfaces[0].Number;
device.ClaimInterface(interfaceNumber);

var reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
var writer = new SynchronizedUsbWriter(device.OpenEndpointWriter(WriteEndpointID.Ep01));
using var cancellation = new CancellationTokenSource();
using var protocolState = new GipProtocolState();
byte messageId = 0;
byte gipSequence = 1;

Console.WriteLine($"Opened Xbox portal {VendorId:X4}:{XboxProductId:X4}, interface {interfaceNumber}, endpoints 81/01.");
Console.WriteLine("Incoming packets will be printed as hex. Type 'help' for commands.");

var readTask = Task.Run(() => ReadPackets(reader, writer, protocolState, cancellation.Token));

try
{
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input is null || input.Equals("quit", StringComparison.OrdinalIgnoreCase) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            continue;
        }

        try
        {
            switch (parts[0].ToLowerInvariant())
            {
                case "help":
                    PrintHelp();
                    break;
                case "gip-init":
                    protocolState.GatewayActivated.Reset();
                    SendLegoMessage(writer, new Message(MessageCommand.Wake), "(c) LEGO 2014", ref messageId, ref gipSequence);
                    if (!protocolState.GatewayActivated.Wait(WakeProbeTimeout))
                    {
                        SendGip(writer, 0x06, 0x20, NextGipSequence(ref gipSequence), [0x01, 0x00]);
                        WaitForProtocolEvent(protocolState.GatewayActivated, "LEGO gateway activation");
                    }
                    break;
                case "gip-auth-done":
                    SendGip(writer, 0x06, 0x20, NextGipSequence(ref gipSequence), [0x01, 0x00]);
                    break;
                case "gip-identify":
                    SendIdentify(writer);
                    break;
                case "gip":
                    SendGipCommand(writer, parts.ElementAtOrDefault(1), ref gipSequence);
                    break;
                case "wake":
                case "test-wake":
                    SendLegoMessage(writer, new Message(MessageCommand.Wake), "(c) LEGO 2014", ref messageId, ref gipSequence);
                    break;
                case "message":
                    SendMessage(writer, parts.ElementAtOrDefault(1), ref messageId, ref gipSequence);
                    break;
                case "test-seed":
                    SendTestMessage(writer, MessageCommand.Seed, [0xAA, 0x6F, 0xC8, 0xCD, 0x21, 0x1E, 0xF8, 0xCE], ref messageId, ref gipSequence);
                    break;
                case "test-challenge":
                    SendTestMessage(writer, MessageCommand.Challenge, [], ref messageId, ref gipSequence);
                    break;
                case "test-color":
                    SendTestMessage(writer, MessageCommand.Color, [0x01, 0xFF, 0x00, 0x00], ref messageId, ref gipSequence);
                    break;
                case "test-get-color":
                    SendTestMessage(writer, MessageCommand.GetColor, [0x01], ref messageId, ref gipSequence);
                    break;
                case "test-fade":
                    SendTestMessage(writer, MessageCommand.Fade, [0x01, 0x32, 0x05, 0xFF, 0x00, 0x00], ref messageId, ref gipSequence);
                    break;
                case "test-flash":
                    SendTestMessage(writer, MessageCommand.Flash, [0x03, 0x14, 0x14, 0x14, 0xF0, 0xFF, 0xFF], ref messageId, ref gipSequence);
                    break;
                case "test-fade-random":
                    SendTestMessage(writer, MessageCommand.FadeRandom, [0x02, 0x0A, 0x0A], ref messageId, ref gipSequence);
                    break;
                case "test-fade-all":
                    SendTestMessage(writer, MessageCommand.FadeAll, [0x01, 0x32, 0x05, 0xFF, 0x00, 0x00, 0x01, 0x05, 0x32, 0x00, 0x80, 0x00, 0x01, 0x0A, 0x64, 0xFF, 0xFF, 0x00], ref messageId, ref gipSequence);
                    break;
                case "test-flash-all":
                    SendTestMessage(writer, MessageCommand.FlashAll, [0x01, 0x0A, 0x1E, 0x28, 0xFF, 0x00, 0x00, 0x01, 0x01, 0x01, 0xFF, 0x00, 0x80, 0x00, 0x01, 0x14, 0x14, 0x14, 0xF0, 0xFF, 0xFF], ref messageId, ref gipSequence);
                    break;
                case "test-color-all":
                    SendTestMessage(writer, MessageCommand.ColorAll, [0x01, 0xFF, 0x00, 0x00, 0x01, 0x00, 0xFF, 0x00, 0x01, 0x00, 0x00, 0xFF], ref messageId, ref gipSequence);
                    break;
                case "test-color-off":
                    SendTestMessage(writer, MessageCommand.ColorAll, [0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00], ref messageId, ref gipSequence);
                    break;
                case "test-list-tags":
                    SendTestMessage(writer, MessageCommand.TagList, [], ref messageId, ref gipSequence);
                    break;
                case "test-read":
                    SendFixedTestMessage(writer, MessageCommand.Read, parts.ElementAtOrDefault(1), [0x00, 0x24], 2, "Usage: test-read [index page]", ref messageId, ref gipSequence);
                    break;
                case "test-write":
                    SendFixedTestMessage(writer, MessageCommand.Write, parts.ElementAtOrDefault(1), null, 6, "Usage: test-write <index page byte0 byte1 byte2 byte3>", ref messageId, ref gipSequence);
                    break;
                case "test-model":
                    SendFixedTestMessage(writer, MessageCommand.Model, parts.ElementAtOrDefault(1), null, 8, "Usage: test-model <8 encrypted bytes>", ref messageId, ref gipSequence);
                    break;
                case "test-password-auto":
                    SendPasswordAutoTest(writer, parts.ElementAtOrDefault(1), ref messageId, ref gipSequence);
                    break;
                case "test-nfc-on":
                    SendTestMessage(writer, MessageCommand.ConfigActive, [0x01], ref messageId, ref gipSequence);
                    break;
                case "test-nfc-off":
                    SendTestMessage(writer, MessageCommand.ConfigActive, [0x00], ref messageId, ref gipSequence);
                    break;
                case "send":
                    Send(writer, ParseHex(parts.ElementAtOrDefault(1)));
                    break;
                default:
                    Console.WriteLine("Unknown command. Type 'help' for usage.");
                    break;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Command error: {exception.Message}");
        }
    }
}
finally
{
    cancellation.Cancel();
    await readTask;
    device.ReleaseInterface(interfaceNumber);
    device.Close();
}

return 0;

static void ReadPackets(UsbEndpointReader reader, SynchronizedUsbWriter writer, GipProtocolState protocolState, CancellationToken cancellationToken)
{
    var buffer = new byte[1024];
    byte[]? previousPacket = null;
    var repeatCount = 0;
    GipChunkAssembly? chunkAssembly = null;

    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            var errorCode = reader.Read(buffer, TransferTimeout, out var bytesRead);
            if (errorCode == Error.Timeout)
            {
                continue;
            }

            if (errorCode != Error.Success)
            {
                Console.WriteLine($"\nRead error: {errorCode}");
                break;
            }

            if (bytesRead > 0)
            {
                var offset = 0;
                while (offset < bytesRead)
                {
                    var packetLength = GetGipPacketLength(buffer.AsSpan(offset, bytesRead - offset));
                    if (packetLength == 0)
                    {
                        Console.WriteLine($"\nRX undecoded ({bytesRead - offset}): {Convert.ToHexString(buffer, offset, bytesRead - offset)}");
                        break;
                    }

                    var packet = buffer.AsSpan(offset, packetLength).ToArray();
                    offset += packetLength;
                    ObserveProtocolPacket(packet, protocolState);

                    if (previousPacket is not null && packet.AsSpan().SequenceEqual(previousPacket))
                    {
                        repeatCount++;
                        continue;
                    }

                    if (repeatCount > 0)
                    {
                        Console.WriteLine($"\nRX ({previousPacket!.Length}): {Convert.ToHexString(previousPacket)} repeated {repeatCount} additional time(s).");
                    }

                    Console.WriteLine($"\nRX ({packetLength}): {Convert.ToHexString(packet)}");
                    PrintGipPacket(packet);
                    ProcessGipChunk(packet, writer, protocolState, ref chunkAssembly);
                    previousPacket = packet;
                    repeatCount = 0;
                }

            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"\nRead error: {exception.Message}");
        }
    }
}

static void ProcessGipChunk(byte[] packet, SynchronizedUsbWriter writer, GipProtocolState protocolState, ref GipChunkAssembly? assembly)
{
    if (!TryReadGipHeader(packet, out var header))
    {
        return;
    }

    if ((header.Options & 0xC0) == 0xC0)
    {
        assembly = new GipChunkAssembly(header.Command, header.ChunkValue, packet.AsSpan(header.HeaderLength, header.PayloadLength));
        Console.WriteLine($"  Chunk transfer: {assembly.Received}/{assembly.Data.Length} byte(s)");
    }
    else if ((header.Options & 0x80) != 0 && (header.Options & 0x40) == 0 && assembly is not null && assembly.Command == header.Command)
    {
        assembly.Add(header.ChunkValue, packet.AsSpan(header.HeaderLength, header.PayloadLength));
        Console.WriteLine($"  Chunk transfer: {assembly.Received}/{assembly.Data.Length} byte(s)");
    }

    if ((header.Options & 0x10) != 0)
    {
        var received = (header.Options & 0x40) != 0 ? header.PayloadLength : header.ChunkValue + header.PayloadLength;
        var total = assembly?.Data.Length ?? received;
        SendGipAcknowledgement(writer, header, received, Math.Max(0, total - received));
    }

    if (assembly is not null && assembly.Received == assembly.Data.Length)
    {
        Console.WriteLine($"GIP {GetGipCommandName(assembly.Command)} reassembled ({assembly.Data.Length}): {Convert.ToHexString(assembly.Data)}");
        if (assembly.Command == 0x04)
        {
            protocolState.IdentificationCompleted.Set();
        }

        assembly = null;
    }
}

static void WaitForProtocolEvent(ManualResetEventSlim protocolEvent, string description)
{
    if (!protocolEvent.Wait(TimeSpan.FromSeconds(3)))
    {
        throw new IOException($"Timed out waiting for {description}.");
    }
}

static void ObserveProtocolPacket(ReadOnlySpan<byte> packet, GipProtocolState protocolState)
{
    if (!TryReadGipHeader(packet, out var header))
    {
        return;
    }

    var payload = packet[header.HeaderLength..];
    if (header.Command == 0x02)
    {
        protocolState.ObserveAnnounce();
    }
    else if (header.Command == 0x01 && payload.Length >= 2)
    {
        if (payload[1] == 0x05)
        {
            protocolState.PowerAcknowledged.Set();
        }
    }
    else if (header.Command == 0x21)
    {
        protocolState.MarkGatewayActivated();
    }
}

static void SendGipAcknowledgement(SynchronizedUsbWriter writer, GipHeader acknowledged, int received, int remaining)
{
    byte[] payload = new byte[9];
    payload[1] = acknowledged.Command;
    payload[2] = (byte)(0x20 | (acknowledged.Options & 0x0F));
    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(3), checked((ushort)received));
    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(7), checked((ushort)remaining));
    byte[] packet = [0x01, (byte)(0x20 | (acknowledged.Options & 0x0F)), acknowledged.Sequence, 0x09, .. payload];

    Send(writer, packet);
}

static bool TryReadGipHeader(ReadOnlySpan<byte> packet, out GipHeader header)
{
    header = default;
    if (packet.Length < 4)
    {
        return false;
    }

    var offset = 3;
    var payloadLength = DecodeGipVarInt(packet, ref offset);
    if (payloadLength < 0)
    {
        return false;
    }

    var chunkValue = 0;
    if ((packet[1] & 0x80) != 0)
    {
        chunkValue = DecodeGipVarInt(packet, ref offset);
        if (chunkValue < 0)
        {
            return false;
        }
    }

    if (offset + payloadLength > packet.Length)
    {
        return false;
    }

    header = new GipHeader(packet[0], packet[1], packet[2], payloadLength, chunkValue, offset);
    return true;
}

static int GetGipPacketLength(ReadOnlySpan<byte> data)
{
    if (data.Length < 4)
    {
        return 0;
    }

    var headerLength = 3;
    var payloadLength = DecodeGipVarInt(data, ref headerLength);
    if (payloadLength < 0)
    {
        return 0;
    }

    if ((data[1] & 0x80) != 0 && DecodeGipVarInt(data, ref headerLength) < 0)
    {
        return 0;
    }

    var packetLength = headerLength + payloadLength;
    return packetLength <= data.Length ? packetLength : 0;
}

static int DecodeGipVarInt(ReadOnlySpan<byte> data, ref int offset)
{
    var value = 0;
    for (var shift = 0; shift < 28 && offset < data.Length; shift += 7)
    {
        var current = data[offset++];
        value |= (current & 0x7F) << shift;
        if ((current & 0x80) == 0)
        {
            return value;
        }
    }

    return -1;
}

static void PrintGipPacket(byte[] packet)
{
    if (!TryReadGipHeader(packet, out var header))
    {
        return;
    }

    var command = header.Command;
    var options = header.Options;
    var commandName = GetGipCommandName(command);

    var length = header.PayloadLength;
    Console.WriteLine($"GIP {commandName}: client={options & 0x0F}, options=0x{options & 0xF0:X2}, sequence={header.Sequence}, payload={length} byte(s)");

    if ((options & 0x80) != 0)
    {
        Console.WriteLine($"  Chunk:     {((options & 0x40) != 0 ? "start, total" : "offset")}={header.ChunkValue}");
    }

    if (command == 0x02 && length == 28)
    {
        var payload = packet.AsSpan(header.HeaderLength, length);
        Console.WriteLine($"  Address:  {BitConverter.ToString(payload[..6].ToArray())}");
        Console.WriteLine($"  Device:   {BinaryPrimitives.ReadUInt16LittleEndian(payload[8..]):X4}:{BinaryPrimitives.ReadUInt16LittleEndian(payload[10..]):X4}");
        Console.WriteLine($"  Firmware: {FormatGipVersion(payload[12..20])}");
        Console.WriteLine($"  Hardware: {FormatGipVersion(payload[20..28])}");
    }
    else if (command == 0x21 && length == 32)
    {
        PrintLegoMessage(packet.AsSpan(header.HeaderLength, length));
    }
}

static void PrintLegoMessage(ReadOnlySpan<byte> payload)
{
    try
    {
        var message = Message.CreateFromBuffer(payload.ToArray(), MessageSource.Portal);
        if (message.MessageType == MessageType.Event && message.Payload.Length >= 11)
        {
            var present = message.Payload[3] == 0;
            Console.WriteLine($"  LEGO event: pad={message.Payload[0]}, type=0x{message.Payload[1]:X2}, index={message.Payload[2]}, present={present}");
            Console.WriteLine($"  Tag UID:    {BitConverter.ToString(message.Payload, 4, 7)}");
        }
        else
        {
            Console.WriteLine($"  LEGO response: id={message.MessageId}, payload={Convert.ToHexString(message.Payload)}");
        }
    }
    catch (ArgumentException exception)
    {
        Console.WriteLine($"  Invalid LEGO frame: {exception.Message}");
    }
}

static void DumpDescriptor(string label, object descriptor)
{
    var values = descriptor.GetType().GetProperties()
        .Where(property => property.PropertyType.IsPrimitive || property.PropertyType.IsEnum || property.PropertyType == typeof(string))
        .Select(property => $"{property.Name}={property.GetValue(descriptor)}");
    Console.WriteLine($"{label}: {string.Join(", ", values)}");
}

static int DescribeDevice(UsbContext context, int vendorId, int productId)
{
    var matchingDevices = context.List()
        .Where(device => device.VendorId == vendorId && device.ProductId == productId)
        .ToArray();
    if (matchingDevices.Length == 0)
    {
        Console.Error.WriteLine($"Device {vendorId:X4}:{productId:X4} was not found by libusb.");
        return 1;
    }

    foreach (var matchingDevice in matchingDevices)
    {
        Console.WriteLine($"Device {matchingDevice.VendorId:X4}:{matchingDevice.ProductId:X4}");
        foreach (var deviceConfiguration in matchingDevice.Configs)
        {
            DumpDescriptor("  Configuration", deviceConfiguration);
            foreach (var deviceInterface in deviceConfiguration.Interfaces)
            {
                DumpDescriptor("    Interface", deviceInterface);
                foreach (var deviceEndpoint in deviceInterface.Endpoints)
                {
                    DumpDescriptor("      Endpoint", deviceEndpoint);
                }
            }
        }
    }

    return 0;
}

static int ProbeXbox360(UsbContext context, int vendorId, int productId)
{
    var devices = context.List()
        .Where(device => device.VendorId == vendorId && device.ProductId == productId)
        .ToArray();
    if (devices.Length == 0)
    {
        Console.Error.WriteLine($"Device {vendorId:X4}:{productId:X4} was not found by libusb.");
        return 1;
    }

    using var device = devices[0];
    try
    {
        device.Open();
    }
    catch (UsbException exception)
    {
        Console.Error.WriteLine($"Device {vendorId:X4}:{productId:X4} could not be opened: {exception.Message}");
        return 3;
    }

    var configuration = device.Configs[0];

    // toypad.py's init() skips reset()/set_configuration() entirely on win32; only call it elsewhere.
    if (!OperatingSystem.IsWindows())
    {
        try
        {
            device.SetConfiguration(configuration.ConfigurationValue);
        }
        catch (UsbException exception)
        {
            Console.WriteLine($"USB configuration {configuration.ConfigurationValue} could not be selected; continuing: {exception.Message}");
        }
    }

    var interfaceNumber = configuration.Interfaces[0].Number;
    var securityInterfaceNumber = configuration.Interfaces.Single(interfaceInfo => interfaceInfo.Number == 3).Number;
    device.ClaimInterface(interfaceNumber);
    device.ClaimInterface(securityInterfaceNumber);

    // A background polling thread reading concurrently with writes on another thread silently loses
    // replies (confirmed via hybrid-async-wake-360); every command below does write-then-read on this
    // one thread instead, with no other thread ever touching the device handle at the same time.
    var rawHandle = device.DeviceHandle.DangerousGetHandle();
    byte messageId = 0;
    PortalRng? xbox360Rng = null;

    Console.WriteLine($"Opened device {vendorId:X4}:{productId:X4}, interface {interfaceNumber}, endpoints 81/01.");
    Console.WriteLine("Type 'help' for commands.");

    try
    {
        while (true)
        {
            Console.Write("360> ");
            var input = ReadLineWhilePollingForEvents(rawHandle);
            if (input is null || input.Equals("quit", StringComparison.OrdinalIgnoreCase) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            try
            {
                switch (parts[0].ToLowerInvariant())
                {
                    case "help":
                        Console.WriteLine("send <hex>                              Send exact bytes to endpoint 01");
                        Console.WriteLine("wake                                    Send an Xbox 360-wrapped LEGO wake frame; waits for and decodes the start reply");
                        Console.WriteLine("xinput-led                              Send Xbox 360 LED command 01-03-01 (confirmed by a real console MITM capture)");
                        Console.WriteLine("test-color                              Send a wrapped LEGO Color command (C0), red on the center pad");
                        Console.WriteLine("test-get-color                          Request center pad color (C1); waits briefly for a reply");
                        Console.WriteLine("test-fade                               Fade center pad to red (C2)");
                        Console.WriteLine("test-flash                              Flash right pad azure (C3)");
                        Console.WriteLine("test-fade-random                        Random-fade left pad (C4)");
                        Console.WriteLine("test-fade-all                           Exercise different fades on all pads (C6)");
                        Console.WriteLine("test-flash-all                          Exercise different flashes on all pads (C7)");
                        Console.WriteLine("test-color-all                          Set center red, left green, right blue (C8)");
                        Console.WriteLine("test-color-off                          Switch all pad LEDs off (C8)");
                        Console.WriteLine("test-list-tags                          List tags on the portal (D0); waits briefly for a reply");
                        Console.WriteLine("test-read [index page]                  Read 16 bytes; defaults to index 00, page 24 (D2); waits briefly for a reply");
                        Console.WriteLine("test-seed [seed-hex] [nonce-hex]        Send a TEA-encrypted seed (B1) and verify the echo");
                        Console.WriteLine("test-challenge [8-byte-hex]             Send a challenge (B3) and verify the RNG-derived reply");
                        Console.WriteLine("test-challenge-loop [count]             Repeat test-challenge with random payloads (default 25)");
                        Console.WriteLine("listen [seconds]                        Listen for unsolicited tag placement/removal events (default 10s)");
                        Console.WriteLine("xsm3-auth                               Complete Xbox 360 security authentication");
                        Console.WriteLine("control-in <type request value index n> Issue a vendor control-IN request; numbers are hex");
                        Console.WriteLine("control-out <type request value index hex> Issue a vendor control-OUT request; numbers are hex");
                        Console.WriteLine("quit                                    Close the device and exit");
                        break;
                    case "send":
                        RawSend(rawHandle, ParseHex(parts.ElementAtOrDefault(1)));
                        break;
                    case "wake":
                        SendXbox360WakeAndReport(rawHandle);
                        break;
                    case "xinput-led":
                        RawSend(rawHandle, [0x01, 0x03, 0x01]);
                        break;
                    case "test-color":
                        SendXbox360Message(rawHandle, MessageCommand.Color, ref messageId, new byte[] { 0x01, 0xFF, 0x00, 0x00 });
                        break;
                    case "test-get-color":
                        SendXbox360MessageAndReport(rawHandle, MessageCommand.GetColor, ref messageId, new byte[] { 0x01 });
                        break;
                    case "test-fade":
                        SendXbox360Message(rawHandle, MessageCommand.Fade, ref messageId, new byte[] { 0x01, 0x32, 0x05, 0xFF, 0x00, 0x00 });
                        break;
                    case "test-flash":
                        SendXbox360Message(rawHandle, MessageCommand.Flash, ref messageId, new byte[] { 0x03, 0x14, 0x14, 0x14, 0xF0, 0xFF, 0xFF });
                        break;
                    case "test-fade-random":
                        SendXbox360Message(rawHandle, MessageCommand.FadeRandom, ref messageId, new byte[] { 0x02, 0x0A, 0x0A });
                        break;
                    case "test-fade-all":
                        SendXbox360Message(rawHandle, MessageCommand.FadeAll, ref messageId, new byte[] { 0x01, 0x32, 0x05, 0xFF, 0x00, 0x00, 0x01, 0x05, 0x32, 0x00, 0x80, 0x00, 0x01, 0x0A, 0x64, 0xFF, 0xFF, 0x00 });
                        break;
                    case "test-flash-all":
                        SendXbox360Message(rawHandle, MessageCommand.FlashAll, ref messageId, new byte[] { 0x01, 0x0A, 0x1E, 0x28, 0xFF, 0x00, 0x00, 0x01, 0x01, 0x01, 0xFF, 0x00, 0x80, 0x00, 0x01, 0x14, 0x14, 0x14, 0xF0, 0xFF, 0xFF });
                        break;
                    case "test-color-all":
                        SendXbox360Message(rawHandle, MessageCommand.ColorAll, ref messageId, new byte[] { 0x01, 0xFF, 0x00, 0x00, 0x01, 0x00, 0xFF, 0x00, 0x01, 0x00, 0x00, 0xFF });
                        break;
                    case "test-color-off":
                        SendXbox360Message(rawHandle, MessageCommand.ColorAll, ref messageId, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 });
                        break;
                    case "test-list-tags":
                        SendXbox360MessageAndReport(rawHandle, MessageCommand.TagList, ref messageId);
                        break;
                    case "test-read":
                        var readPayload = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(1)) ? new byte[] { 0x00, 0x24 } : ParseHex(parts.ElementAtOrDefault(1));
                        if (readPayload.Length != 2)
                        {
                            throw new ArgumentException("Usage: test-read [index page]");
                        }

                        SendXbox360MessageAndReport(rawHandle, MessageCommand.Read, ref messageId, readPayload);
                        break;
                    case "xsm3-auth":
                        AuthenticateXbox360(device);
                        break;
                    case "control-in":
                        SendControlIn(device, parts.ElementAtOrDefault(1));
                        break;
                    case "control-out":
                        SendControlOut(device, parts.ElementAtOrDefault(1));
                        break;
                    case "test-seed":
                        xbox360Rng = TestXbox360Seed(rawHandle, parts.ElementAtOrDefault(1), ref messageId);
                        break;
                    case "test-challenge":
                        TestXbox360Challenge(rawHandle, xbox360Rng, parts.ElementAtOrDefault(1), ref messageId);
                        break;
                    case "test-challenge-loop":
                        TestXbox360ChallengeLoop(rawHandle, xbox360Rng, parts.ElementAtOrDefault(1), ref messageId);
                        break;
                    case "listen":
                        var listenSeconds = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(1)) ? 10 : int.Parse(parts.ElementAtOrDefault(1)!);
                        ListenForXbox360Events(rawHandle, TimeSpan.FromSeconds(listenSeconds));
                        break;
                    default:
                        Console.WriteLine("Unknown command. Type 'help' for usage.");
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Command error: {exception.Message}");
            }
        }
    }
    finally
    {
        device.ReleaseInterface(securityInterfaceNumber);
        device.ReleaseInterface(interfaceNumber);
        device.Close();
    }

    return 0;
}

static void RawSend(IntPtr rawHandle, byte[] bytes)
{
    if (bytes.Length == 0)
    {
        throw new ArgumentException("No bytes were supplied.");
    }

    var result = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x01, bytes, bytes.Length, out var written, 1000);
    if (result != 0 || written != bytes.Length)
    {
        Console.WriteLine($"USB endpoint 0x01 wrote {written} of {bytes.Length} bytes (result={result}); clearing the halt and retrying.");
        RawLibUsb.libusb_clear_halt(rawHandle, 0x01);
        result = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x01, bytes, bytes.Length, out written, 1000);
    }

    if (result != 0 || written != bytes.Length)
    {
        throw new IOException($"USB endpoint 0x01 wrote {written} of {bytes.Length} bytes after recovery (result={result}). Reconnect the portal.");
    }

    Console.WriteLine($"TX ({written}): {Convert.ToHexString(bytes, 0, written)}");
}

static byte[]? RawReadReply(IntPtr rawHandle, TimeSpan timeout)
{
    // Matches the exact buffer size used by the proven-working raw-wake-360/hybrid-wake-360 tests.
    var buffer = new byte[32];
    var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
    while (Environment.TickCount64 < deadline)
    {
        var result = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x81, buffer, buffer.Length, out var read, TransferTimeout);
        if (result == 0 && read > 0)
        {
            var packet = buffer[..read];
            Console.WriteLine($"RX ({read}): {Convert.ToHexString(packet)}");
            return packet;
        }

        if (result != 0 && result != RawLibUsb.ErrorTimeout)
        {
            Console.WriteLine($"RX error={result}");
            return null;
        }
    }

    return null;
}

static Message? ReadXbox360LegoReply(byte[] rawFrame)
{
    if (rawFrame.Length < 2 || rawFrame[0] != 0x0B || rawFrame[1] != 0x16)
    {
        Console.WriteLine($"Unexpected frame prefix: {Convert.ToHexString(rawFrame)}");
        return null;
    }

    var standardFrame = new byte[32];
    rawFrame.AsSpan(2, Math.Min(30, rawFrame.Length - 2)).CopyTo(standardFrame);
    try
    {
        return Message.CreateFromBuffer(standardFrame, MessageSource.Portal);
    }
    catch (ArgumentException exception)
    {
        // Message.CreateFromBuffer throws on an invalid type, payload size, or checksum; don't let
        // one malformed frame take down a polling loop that has no other try/catch around it.
        Console.WriteLine($"Malformed frame ({exception.Message}): {Convert.ToHexString(rawFrame)}");
        return null;
    }
}

static string? ReadLineWhilePollingForEvents(IntPtr rawHandle)
{
    // Console.ReadLine() runs on its own thread and never touches libusb; this thread polls for
    // unsolicited events only between checks of that task, so at most one thread ever calls
    // libusb at a time (concurrent read+write from separate threads silently loses replies).
    var inputTask = Task.Run(Console.ReadLine);
    while (!inputTask.IsCompleted)
    {
        var buffer = new byte[32];
        var result = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x81, buffer, buffer.Length, out var read, 200);
        if (result == 0 && read > 0)
        {
            var frame = ReadXbox360LegoReply(buffer[..read]);
            if (frame is not null)
            {
                Console.WriteLine();
                PrintXbox360LegoFrame(frame);
                Console.Write("360> ");
            }
        }
    }

    return inputTask.Result;
}

static void ListenForXbox360Events(IntPtr rawHandle, TimeSpan duration)
{
    Console.WriteLine($"Listening for {duration.TotalSeconds:0}s; place or remove a tag now.");
    var buffer = new byte[32];
    var deadline = Environment.TickCount64 + (long)duration.TotalMilliseconds;
    while (Environment.TickCount64 < deadline)
    {
        var result = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x81, buffer, buffer.Length, out var read, TransferTimeout);
        if (result == 0 && read > 0)
        {
            var frame = ReadXbox360LegoReply(buffer[..read]);
            if (frame is not null)
            {
                PrintXbox360LegoFrame(frame);
            }
        }
        else if (result != 0 && result != RawLibUsb.ErrorTimeout)
        {
            Console.WriteLine($"Listen read error={result}");
            break;
        }
    }

    Console.WriteLine("Done listening.");
}

static void PrintXbox360LegoFrame(Message message)
{
    if (message.MessageType == MessageType.Event && message.Payload.Length >= 11)
    {
        var present = message.Payload[3] == 0;
        Console.WriteLine($"  LEGO event: pad={message.Payload[0]}, type=0x{message.Payload[1]:X2}, index={message.Payload[2]}, present={present}");
        Console.WriteLine($"  Tag UID:    {BitConverter.ToString(message.Payload, 4, 7)}");
    }
    else
    {
        Console.WriteLine($"  LEGO response: id={message.MessageId}, payload={Convert.ToHexString(message.Payload)}");
    }
}

static void SendXbox360Message(IntPtr rawHandle, MessageCommand command, ref byte messageId, params object[] payload)
{
    var message = new Message(command);
    if (payload.Length > 0)
    {
        message.AddPayload(payload);
    }

    RawSend(rawHandle, Xbox360Transport.WrapLegoFrame(message.GetBytes(NextMessageId(ref messageId))));
}

static void SendXbox360MessageAndReport(IntPtr rawHandle, MessageCommand command, ref byte messageId, params object[] payload)
{
    var message = new Message(command);
    if (payload.Length > 0)
    {
        message.AddPayload(payload);
    }

    RawSend(rawHandle, Xbox360Transport.WrapLegoFrame(message.GetBytes(NextMessageId(ref messageId))));

    var rawReply = RawReadReply(rawHandle, TimeSpan.FromSeconds(2));
    if (rawReply is null)
    {
        Console.WriteLine("No reply received within 2 seconds.");
        return;
    }

    var reply = ReadXbox360LegoReply(rawReply);
    if (reply is not null)
    {
        Console.WriteLine($"Reply payload: {Convert.ToHexString(reply.Payload)}");
    }
}

static void SendXbox360WakeAndReport(IntPtr rawHandle)
{
    var message = new Message(MessageCommand.Wake);
    message.AddPayload("(c) LEGO 2014");

    // toypad.py's start() hardcodes message_id=0 for wake; our shared counter never emits 0, so match it explicitly.
    RawSend(rawHandle, Xbox360Transport.WrapLegoFrame(message.GetBytes(0)));

    // dopheideb/LEGODimensions' toypad.py waits up to 5s and treats wake as request/reply, not fire-and-forget.
    var rawReply = RawReadReply(rawHandle, TimeSpan.FromSeconds(5));
    if (rawReply is null)
    {
        Console.WriteLine("No reply received within 5 seconds.");
        return;
    }

    var reply = ReadXbox360LegoReply(rawReply);
    if (reply is null || reply.Payload.Length < 24)
    {
        Console.WriteLine($"Unexpected wake reply payload length: {reply?.Payload.Length ?? -1}.");
        return;
    }

    // Field layout matches toypad.py's start(): status, static bytes, then LPC UID split across 4 words.
    var status = reply.Payload[0];
    var staticBytes = reply.Payload.AsSpan(2, 6);
    var uidPart3 = reply.Payload.AsSpan(8, 4);
    var uidPart2 = reply.Payload.AsSpan(12, 4);
    var uidPart1 = reply.Payload.AsSpan(16, 4);
    var uidPart0 = reply.Payload.AsSpan(20, 4);
    Console.WriteLine($"Wake reply: messageId={reply.MessageId:X2} status={status:X2} static={Convert.ToHexString(staticBytes)} uid={Convert.ToHexString(uidPart0)}{Convert.ToHexString(uidPart1)}{Convert.ToHexString(uidPart2)}{Convert.ToHexString(uidPart3)}");
    if (status != 0x00)
    {
        Console.WriteLine("Toypad reports the wake/start command as invalid (status != 0x00).");
    }
}

static PortalRng? TestXbox360Seed(IntPtr rawHandle, string? arguments, ref byte messageId)
{
    var fields = arguments?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    var seed = fields.Length > 0 ? Convert.ToUInt32(fields[0], 16) : (uint)Random.Shared.NextInt64(uint.MaxValue);
    var nonce = fields.Length > 1 ? Convert.ToUInt32(fields[1], 16) : (uint)Random.Shared.NextInt64(uint.MaxValue);

    Span<byte> plaintext = stackalloc byte[8];
    BinaryPrimitives.WriteUInt32LittleEndian(plaintext, seed);
    BinaryPrimitives.WriteUInt32LittleEndian(plaintext[4..], nonce);
    var encrypted = PortalTea.Encrypt(PortalTea.SeedKey, plaintext);

    var message = new Message(MessageCommand.Seed);
    message.AddPayload(encrypted);

    Console.WriteLine($"Seed test: seed=0x{seed:X8} nonce=0x{nonce:X8} encrypted={Convert.ToHexString(encrypted)}");
    RawSend(rawHandle, Xbox360Transport.WrapLegoFrame(message.GetBytes(NextMessageId(ref messageId))));

    var rawReply = RawReadReply(rawHandle, TimeSpan.FromSeconds(2));
    if (rawReply is null)
    {
        Console.WriteLine("No reply received within 2 seconds.");
        return null;
    }

    var reply = ReadXbox360LegoReply(rawReply);
    if (reply is null || reply.Payload.Length != 8)
    {
        Console.WriteLine($"Unexpected seed reply payload length: {reply?.Payload.Length ?? -1}.");
        return null;
    }

    var decryptedReply = PortalTea.Decrypt(PortalTea.SeedKey, reply.Payload);
    var echoedNonce = BinaryPrimitives.ReadUInt32LittleEndian(decryptedReply);
    if (echoedNonce != nonce)
    {
        Console.WriteLine($"Seed echo mismatch: expected nonce 0x{nonce:X8}, got 0x{echoedNonce:X8}.");
        return null;
    }

    Console.WriteLine("Seed accepted; nonce echo matches. The portal's RNG is now seeded.");
    return new PortalRng(seed);
}

static void TestXbox360Challenge(IntPtr rawHandle, PortalRng? rng, string? arguments, ref byte messageId)
{
    if (rng is null)
    {
        throw new InvalidOperationException("Run test-seed first so the expected RNG output can be predicted.");
    }

    var challengePayload = string.IsNullOrWhiteSpace(arguments) ? RandomNumberGenerator.GetBytes(8) : ParseHex(arguments);
    if (challengePayload.Length != 8)
    {
        throw new ArgumentException("Usage: test-challenge [8-byte-hex-payload]");
    }

    TryRunXbox360Challenge(rawHandle, rng, challengePayload, ref messageId);
}

static void TestXbox360ChallengeLoop(IntPtr rawHandle, PortalRng? rng, string? arguments, ref byte messageId)
{
    if (rng is null)
    {
        throw new InvalidOperationException("Run test-seed first so the expected RNG output can be predicted.");
    }

    // 25 matches dopheideb/LEGODimensions' NUM_CHALLENGES_PER_SEED soak-test constant.
    var count = string.IsNullOrWhiteSpace(arguments) ? 25 : int.Parse(arguments);
    var passed = 0;
    for (var i = 1; i <= count; i++)
    {
        Console.WriteLine($"[{i}/{count}]");
        if (TryRunXbox360Challenge(rawHandle, rng, RandomNumberGenerator.GetBytes(8), ref messageId) == true)
        {
            passed++;
        }
    }

    Console.WriteLine($"Challenge loop complete: {passed}/{count} matched.");
}

static bool? TryRunXbox360Challenge(IntPtr rawHandle, PortalRng rng, byte[] challengePayload, ref byte messageId)
{
    var message = new Message(MessageCommand.Challenge);
    message.AddPayload(challengePayload);

    RawSend(rawHandle, Xbox360Transport.WrapLegoFrame(message.GetBytes(NextMessageId(ref messageId))));

    var rawReply = RawReadReply(rawHandle, TimeSpan.FromSeconds(2));
    if (rawReply is null)
    {
        Console.WriteLine("No reply received within 2 seconds.");
        return null;
    }

    var reply = ReadXbox360LegoReply(rawReply);
    if (reply is null || reply.Payload.Length != 8)
    {
        Console.WriteLine($"Unexpected challenge reply payload length: {reply?.Payload.Length ?? -1}.");
        return null;
    }

    var decryptedChallenge = PortalTea.Decrypt(PortalTea.SeedKey, challengePayload);
    Span<byte> expectedPlaintext = stackalloc byte[8];
    BinaryPrimitives.WriteUInt32LittleEndian(expectedPlaintext, rng.Next());
    decryptedChallenge.AsSpan(0, 4).CopyTo(expectedPlaintext[4..]);
    var expectedReply = PortalTea.Encrypt(PortalTea.SeedKey, expectedPlaintext);

    var matches = expectedReply.AsSpan().SequenceEqual(reply.Payload);
    if (matches)
    {
        Console.WriteLine("Challenge reply matches the independently computed RNG output.");
    }
    else
    {
        Console.WriteLine($"Challenge reply MISMATCH: expected {Convert.ToHexString(expectedReply)}, got {Convert.ToHexString(reply.Payload)}.");
    }

    return matches;
}

static void SendControlIn(IUsbDevice device, string? arguments)
{
    var fields = arguments?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length != 5 || fields.Any(field => !int.TryParse(field, System.Globalization.NumberStyles.HexNumber, null, out _)))
    {
        throw new ArgumentException("Usage: control-in <request-type request value index length>; all numbers are hex");
    }

    var values = fields.Select(field => Convert.ToInt32(field, 16)).ToArray();
    var buffer = ControlIn(device, (byte)values[0], (byte)values[1], values[2], values[3], values[4]);

    Console.WriteLine($"CONTROL RX ({buffer.Length}): {Convert.ToHexString(buffer)}");
}

static void SendControlOut(IUsbDevice device, string? arguments)
{
    var fields = arguments?.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length is < 4 or > 5 || fields.Take(4).Any(field => !int.TryParse(field, System.Globalization.NumberStyles.HexNumber, null, out _)))
    {
        throw new ArgumentException("Usage: control-out <request-type request value index [hex]>; all numbers are hex, hex may be omitted for a zero-length transfer");
    }

    var values = fields.Take(4).Select(field => Convert.ToInt32(field, 16)).ToArray();
    var buffer = fields.Length == 5 ? ParseHex(fields[4]) : [];
    ControlOut(device, (byte)values[0], (byte)values[1], values[2], values[3], buffer);

    Console.WriteLine($"CONTROL TX ({buffer.Length}): {Convert.ToHexString(buffer)}");
}


static void AuthenticateXbox360(IUsbDevice device)
{
    var challenge = Convert.FromHexString(
        "094000001CDEEB918766B0E3C0B26C056DC867E2E7D6A5DC716F211FB43228A0C289");

    Console.WriteLine($"XSM3 81 TX (setup): {FormatSetupPacket(0xC1, 0x81, 0x5B17, 0x0103, 0x1D)}");
    var identity = ControlIn(device, 0xC1, 0x81, 0x5B17, 0x0103, 0x1D);
    Console.WriteLine($"XSM3 81 RX: {Convert.ToHexString(identity)}");

    Console.WriteLine($"XSM3 82 TX (setup): {FormatSetupPacket(0x41, 0x82, 0x0003, 0x0103, challenge.Length)}");
    ControlOut(device, 0x41, 0x82, 0x0003, 0x0103, challenge);
    Console.WriteLine($"XSM3 82 TX (data): {Convert.ToHexString(challenge)}");
    WaitForXbox360SecurityResponse(device);

    Console.WriteLine($"XSM3 83 TX (setup): {FormatSetupPacket(0xC1, 0x83, 0x5C28, 0x0103, 0x2E)}");
    var challengeResponse = ControlIn(device, 0xC1, 0x83, 0x5C28, 0x0103, 0x2E);
    Console.WriteLine($"XSM3 83 RX: {Convert.ToHexString(challengeResponse)}");
    var session = Xbox360Xsm3Host.Create(identity, challenge, challengeResponse);

    // Zero-length ack after phase one, seen on real console<->toypad captures ("Pass?").
    Console.WriteLine($"XSM3 84 TX (setup): {FormatSetupPacket(0x41, 0x84, 0x0003, 0x0103, 0)}");
    ControlOut(device, 0x41, 0x84, 0x0003, 0x0103, []);
    Console.WriteLine("XSM3 84 TX (data): (zero length)");

    // Real captures perform two verify/response rounds before the session is considered complete.
    for (var round = 1; round <= 2; round++)
    {
        var verify = session.CreateVerifyPacket();
        Console.WriteLine($"XSM3 87 TX (round {round}, setup): {FormatSetupPacket(0x41, 0x87, 0x0003, 0x0103, verify.Length)}");
        ControlOut(device, 0x41, 0x87, 0x0003, 0x0103, verify);
        Console.WriteLine($"XSM3 87 TX (round {round}, data): {Convert.ToHexString(verify)}");
        WaitForXbox360SecurityResponse(device);

        Console.WriteLine($"XSM3 83 TX (round {round}, setup): {FormatSetupPacket(0xC1, 0x83, 0x5C10, 0x0103, 0x16)}");
        var verifyResponse = ControlIn(device, 0xC1, 0x83, 0x5C10, 0x0103, 0x16);
        Console.WriteLine($"XSM3 83 RX (round {round}): {Convert.ToHexString(verifyResponse)}");
        session.ValidateFinalResponse(verifyResponse);
    }

    Console.WriteLine("XSM3 authentication completed and verified.");
}

static string FormatSetupPacket(byte requestType, byte request, int value, int index, int length)
{
    var setup = new byte[8];
    setup[0] = requestType;
    setup[1] = request;
    setup[2] = (byte)(value & 0xFF);
    setup[3] = (byte)((value >> 8) & 0xFF);
    setup[4] = (byte)(index & 0xFF);
    setup[5] = (byte)((index >> 8) & 0xFF);
    setup[6] = (byte)(length & 0xFF);
    setup[7] = (byte)((length >> 8) & 0xFF);
    return Convert.ToHexString(setup);
}

static void WaitForXbox360SecurityResponse(IUsbDevice device)
{
    var deadline = Environment.TickCount64 + 2000;
    while (Environment.TickCount64 < deadline)
    {
        Console.WriteLine($"XSM3 86 TX (setup): {FormatSetupPacket(0xC1, 0x86, 0x0000, 0x0103, 2)}");
        var status = ControlIn(device, 0xC1, 0x86, 0x0000, 0x0103, 2);
        Console.WriteLine($"XSM3 86 RX: {Convert.ToHexString(status)}");
        if (status.Length == 2 && status[0] == 0x02 && status[1] == 0x00)
        {
            return;
        }

        Thread.Sleep(25);
    }

    throw new TimeoutException("XSM3 response did not become ready within 2 seconds.");
}

static byte[] ControlIn(
    IUsbDevice device,
    byte requestType,
    byte request,
    int value,
    int index,
    int length)
{
    var buffer = new byte[length];
    device.ControlTransfer(new UsbSetupPacket(requestType, request, value, index, length), buffer, 0, buffer.Length);
    return buffer;
}

static void ControlOut(
    IUsbDevice device,
    byte requestType,
    byte request,
    int value,
    int index,
    byte[] buffer)
{
    device.ControlTransfer(new UsbSetupPacket(requestType, request, value, index, buffer.Length), buffer, 0, buffer.Length);
}

static string GetGipCommandName(byte command) => command switch
    {
        0x01 => "ACKNOWLEDGE",
        0x02 => "ANNOUNCE",
        0x03 => "STATUS",
        0x04 => "IDENTIFY",
        0x05 => "POWER",
        0x06 => "AUTHENTICATE",
        0x07 => "VIRTUAL_KEY",
        0x0A => "LED",
        0x1E => "SERIAL_NUMBER",
        0x20 => "INPUT",
        0x21 => "LEGO_GATEWAY",
        _ => "UNKNOWN"
    };

static string FormatGipVersion(ReadOnlySpan<byte> value) =>
    $"{BinaryPrimitives.ReadUInt16LittleEndian(value)}.{BinaryPrimitives.ReadUInt16LittleEndian(value[2..])}.{BinaryPrimitives.ReadUInt16LittleEndian(value[4..])}.{BinaryPrimitives.ReadUInt16LittleEndian(value[6..])}";

static void SendGipCommand(SynchronizedUsbWriter writer, string? arguments, ref byte sequence)
{
    var fields = arguments?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length == 0 || !byte.TryParse(fields[0], System.Globalization.NumberStyles.HexNumber, null, out var command))
    {
        throw new ArgumentException("Usage: gip <command-byte> [payload-hex]");
    }

    SendGip(writer, command, 0x20, NextGipSequence(ref sequence), fields.Length == 2 ? ParseHex(fields[1]) : []);
}

static void SendIdentify(SynchronizedUsbWriter writer) => SendGip(writer, 0x04, 0x20, 0, []);

static void SendGip(SynchronizedUsbWriter writer, byte command, byte options, byte sequence, byte[] payload)
{
    if (payload.Length > 127)
    {
        throw new ArgumentException("This probe currently supports GIP payloads up to 127 bytes.");
    }

    byte[] packet = [command, options, sequence, (byte)payload.Length, .. payload];
    Send(writer, packet);
}

static void SendMessage(SynchronizedUsbWriter writer, string? arguments, ref byte messageId, ref byte gipSequence)
{
    var fields = arguments?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length == 0 || !byte.TryParse(fields[0], System.Globalization.NumberStyles.HexNumber, null, out var command))
    {
        throw new ArgumentException("Usage: message <command-byte> [payload-hex]");
    }

    var message = new Message((MessageCommand)command);
    if (fields.Length == 2)
    {
        message.AddPayload(ParseHex(fields[1]));
    }

    SendLegoMessage(writer, message, null, ref messageId, ref gipSequence);
}

static void SendTestMessage(SynchronizedUsbWriter writer, MessageCommand command, byte[] payload, ref byte messageId, ref byte gipSequence)
{
    var message = new Message(command);
    message.AddPayload(payload);
    SendLegoMessage(writer, message, null, ref messageId, ref gipSequence);
}

static void SendFixedTestMessage(SynchronizedUsbWriter writer, MessageCommand command, string? arguments, byte[]? defaultPayload, int expectedLength, string usage, ref byte messageId, ref byte gipSequence)
{
    var payload = string.IsNullOrWhiteSpace(arguments) && defaultPayload is not null ? defaultPayload : ParseHex(arguments);
    if (payload.Length != expectedLength)
    {
        throw new ArgumentException(usage);
    }

    SendTestMessage(writer, command, payload, ref messageId, ref gipSequence);
}

static void SendPasswordAutoTest(SynchronizedUsbWriter writer, string? arguments, ref byte messageId, ref byte gipSequence)
{
    var index = ParseHex(arguments);
    if (index.Length > 1)
    {
        throw new ArgumentException("Usage: test-password-auto [index]");
    }

    SendTestMessage(writer, MessageCommand.ConfigPassword, [0x01, index.ElementAtOrDefault(0), 0x00, 0x00, 0x00, 0x00], ref messageId, ref gipSequence);
}

static void SendLegoMessage(SynchronizedUsbWriter writer, Message message, string? payload, ref byte messageId, ref byte gipSequence)
{
    if (payload is not null)
    {
        message.AddPayload(payload);
    }

    SendGip(writer, 0x21, 0x00, NextGipSequence(ref gipSequence), message.GetBytes(NextMessageId(ref messageId)));
}

static void Send(SynchronizedUsbWriter writer, byte[] bytes)
{
    if (bytes.Length == 0)
    {
        throw new ArgumentException("No bytes were supplied.");
    }

    writer.Write(bytes);
}

static byte[] ParseHex(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return [];
    }

    var compact = value.Replace("0x", "", StringComparison.OrdinalIgnoreCase)
        .Replace("-", "")
        .Replace(":", "")
        .Replace(" ", "");

    if (compact.Length % 2 != 0)
    {
        throw new FormatException("Hex input must contain complete bytes.");
    }

    return Convert.FromHexString(compact);
}

static byte NextMessageId(ref byte messageId)
{
    messageId = messageId == byte.MaxValue ? (byte)1 : (byte)(messageId + 1);
    return messageId;
}

static byte NextGipSequence(ref byte sequence)
{
    if (sequence == 0)
    {
        sequence = 1;
    }

    var current = sequence++;
    if (sequence == 0)
    {
        sequence = 1;
    }

    return current;
}

static void PrintHelp()
{
    Console.WriteLine("gip-init                     Send LEGO wake and authenticate only if no response arrives");
    Console.WriteLine("gip-auth-done                Experimentally send GIP authentication-complete");
    Console.WriteLine("gip-identify                 Request the GIP device identification data");
    Console.WriteLine("gip <command> [payload]      Build and send an Xbox GIP packet");
    Console.WriteLine("wake                         Send a LEGO wake command through GIP report 0x21");
    Console.WriteLine("message <command> [payload]  Send a LEGO message through GIP report 0x21");
    Console.WriteLine("test-wake                    Send the standard B0 wake command");
    Console.WriteLine("test-seed                    Send the known experimental B1 seed");
    Console.WriteLine("test-challenge               Request an 8-byte B3 challenge");
    Console.WriteLine("test-color                   Set center pad red (C0)");
    Console.WriteLine("test-get-color               Read center pad color (C1)");
    Console.WriteLine("test-fade                    Fade center pad to red (C2)");
    Console.WriteLine("test-flash                   Flash right pad azure (C3)");
    Console.WriteLine("test-fade-random             Random-fade left pad (C4)");
    Console.WriteLine("test-fade-all                Exercise different fades on all pads (C6)");
    Console.WriteLine("test-flash-all               Exercise different flashes on all pads (C7)");
    Console.WriteLine("test-color-all               Set center red, left green, right blue (C8)");
    Console.WriteLine("test-color-off               Switch all pad LEDs off (C8)");
    Console.WriteLine("test-list-tags               List tags currently on the portal (D0)");
    Console.WriteLine("test-read [index page]        Read 16 bytes; defaults to index 00, page 24 (D2)");
    Console.WriteLine("test-write <i p b0 b1 b2 b3> Write four supplied bytes to a tag (D3, destructive)");
    Console.WriteLine("test-model <8 bytes>          Send supplied encrypted model data (D4)");
    Console.WriteLine("test-password-auto [index]   Reset automatic password mode; defaults to index 00 (E1)");
    Console.WriteLine("test-nfc-on                  Enable NFC readers (E5)");
    Console.WriteLine("test-nfc-off                 Disable NFC readers (E5)");
    Console.WriteLine("send <hex>                   Send exact bytes, e.g. send 01-02-FF");
    Console.WriteLine("quit                         Close the device and exit");
}

static int RunRawWake360Test()
{
    Console.WriteLine($"Native libusb version: {NativeLibUsb.GetVersion()}");

    var initResult = RawLibUsb.libusb_init(out var libUsbContext);
    if (initResult != 0)
    {
        Console.Error.WriteLine($"libusb_init failed: {initResult}");
        return 1;
    }

    try
    {
        var deviceHandle = RawLibUsb.libusb_open_device_with_vid_pid(libUsbContext, Xbox360VendorId, Xbox360ProductId);
        if (deviceHandle == IntPtr.Zero)
        {
            Console.Error.WriteLine("Device 24C6:FA01 could not be opened via raw libusb.");
            return 1;
        }

        try
        {
            var claimResult = RawLibUsb.libusb_claim_interface(deviceHandle, 0);
            if (claimResult != 0)
            {
                Console.Error.WriteLine($"libusb_claim_interface(0) failed: {claimResult}");
                return 1;
            }

            try
            {
                var message = new Message(MessageCommand.Wake);
                message.AddPayload("(c) LEGO 2014");
                var frame = Xbox360Transport.WrapLegoFrame(message.GetBytes(0));

                Console.WriteLine($"RAW TX (32): {Convert.ToHexString(frame)}");
                var writeResult = RawLibUsb.libusb_interrupt_transfer(deviceHandle, 0x01, frame, frame.Length, out var written, 1000);
                Console.WriteLine($"RAW write result={writeResult}, written={written}");

                var deadline = Environment.TickCount64 + 5000;
                while (Environment.TickCount64 < deadline)
                {
                    var readBuffer = new byte[32];
                    var readResult = RawLibUsb.libusb_interrupt_transfer(deviceHandle, 0x81, readBuffer, readBuffer.Length, out var read, 250);
                    if (readResult == 0 && read > 0)
                    {
                        Console.WriteLine($"RAW RX ({read}): {Convert.ToHexString(readBuffer, 0, read)}");
                        return 0;
                    }

                    if (readResult != 0 && readResult != RawLibUsb.ErrorTimeout)
                    {
                        Console.WriteLine($"RAW read error={readResult}");
                    }
                }

                Console.WriteLine("RAW: no reply received within 5 seconds.");
                return 0;
            }
            finally
            {
                RawLibUsb.libusb_release_interface(deviceHandle, 0);
            }
        }
        finally
        {
            RawLibUsb.libusb_close(deviceHandle);
        }
    }
    finally
    {
        RawLibUsb.libusb_exit(libUsbContext);
    }
}

static int RunHybridWake360Test(UsbContext context)
{
    // Opens via LibUsbDotNet (so its background event-handling thread starts, see UsbContext.StartHandlingEvents),
    // but performs the actual transfer with raw libusb calls - isolates whether that thread is the culprit.
    var devices = context.List()
        .Where(candidate => candidate.VendorId == Xbox360VendorId && candidate.ProductId == Xbox360ProductId)
        .ToArray();
    if (devices.Length == 0)
    {
        Console.Error.WriteLine("Device 24C6:FA01 was not found by libusb.");
        return 1;
    }

    using var device = devices[0];
    device.Open();
    device.ClaimInterface(0);
    // ProbeXbox360 also claims the security interface (3) for XSM3; test whether that's the interfering factor.
    var securityInterfaceNumber = device.Configs[0].Interfaces.Single(interfaceInfo => interfaceInfo.Number == 3).Number;
    device.ClaimInterface(securityInterfaceNumber);
    var rawHandle = device.DeviceHandle.DangerousGetHandle();
    Console.WriteLine("Opened via LibUsbDotNet (background event thread running, interfaces 0+3 claimed); performing transfer via raw libusb_interrupt_transfer.");

    var message = new Message(MessageCommand.Wake);
    message.AddPayload("(c) LEGO 2014");
    var frame = Xbox360Transport.WrapLegoFrame(message.GetBytes(0));

    Console.WriteLine($"HYBRID TX (32): {Convert.ToHexString(frame)}");
    var writeResult = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x01, frame, frame.Length, out var written, 1000);
    Console.WriteLine($"HYBRID write result={writeResult}, written={written}");

    var deadline = Environment.TickCount64 + 5000;
    while (Environment.TickCount64 < deadline)
    {
        var readBuffer = new byte[32];
        var readResult = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x81, readBuffer, readBuffer.Length, out var read, 250);
        if (readResult == 0 && read > 0)
        {
            Console.WriteLine($"HYBRID RX ({read}): {Convert.ToHexString(readBuffer, 0, read)}");
            device.ReleaseInterface(securityInterfaceNumber);
            device.ReleaseInterface(0);
            return 0;
        }

        if (readResult != 0 && readResult != RawLibUsb.ErrorTimeout)
        {
            Console.WriteLine($"HYBRID read error={readResult}");
        }
    }

    Console.WriteLine("HYBRID: no reply received within 5 seconds.");
    device.ReleaseInterface(securityInterfaceNumber);
    device.ReleaseInterface(0);
    return 0;
}

static async Task<int> RunHybridAsyncWake360Test(UsbContext context)
{
    // Replicates ProbeXbox360's actual concurrency structure (background polling Task, gated by a
    // semaphore/reset-event, while writes happen from the main flow) but with raw libusb calls -
    // isolates whether that concurrency pattern itself (not UsbEndpointReader/Writer) is the culprit.
    var devices = context.List()
        .Where(candidate => candidate.VendorId == Xbox360VendorId && candidate.ProductId == Xbox360ProductId)
        .ToArray();
    if (devices.Length == 0)
    {
        Console.Error.WriteLine("Device 24C6:FA01 was not found by libusb.");
        return 1;
    }

    using var device = devices[0];
    device.Open();
    device.ClaimInterface(0);
    var securityInterfaceNumber = device.Configs[0].Interfaces.Single(interfaceInfo => interfaceInfo.Number == 3).Number;
    device.ClaimInterface(securityInterfaceNumber);
    var rawHandle = device.DeviceHandle.DangerousGetHandle();
    Console.WriteLine("Opened via LibUsbDotNet; background polling Task + raw libusb_interrupt_transfer, mirroring ProbeXbox360's concurrency.");

    using var cancellation = new CancellationTokenSource();
    using var controlTransferGate = new SemaphoreSlim(1, 1);
    using var interruptReadsEnabled = new ManualResetEventSlim(true);
    using var capturedFrames = new BlockingCollection<byte[]>(boundedCapacity: 16);

    var readTask = Task.Run(() =>
    {
        var buffer = new byte[1024];
        while (!cancellation.Token.IsCancellationRequested)
        {
            try
            {
                interruptReadsEnabled.Wait(cancellation.Token);
                controlTransferGate.Wait(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            int readResult;
            int bytesRead;
            try
            {
                readResult = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x81, buffer, buffer.Length, out bytesRead, (uint)TransferTimeout);
            }
            finally
            {
                controlTransferGate.Release();
            }

            if (readResult == RawLibUsb.ErrorTimeout)
            {
                continue;
            }

            if (readResult == 0 && bytesRead > 0)
            {
                Console.WriteLine($"\nASYNC-HYBRID RX ({bytesRead}): {Convert.ToHexString(buffer, 0, bytesRead)}");
                capturedFrames.TryAdd(buffer[..bytesRead]);
            }
            else if (readResult != 0)
            {
                Console.WriteLine($"\nASYNC-HYBRID read error={readResult}");
                break;
            }
        }
    });

    var message = new Message(MessageCommand.Wake);
    message.AddPayload("(c) LEGO 2014");
    var frame = Xbox360Transport.WrapLegoFrame(message.GetBytes(0));

    while (capturedFrames.TryTake(out _)) { }
    Console.WriteLine($"ASYNC-HYBRID TX (32): {Convert.ToHexString(frame)}");
    var writeResult = RawLibUsb.libusb_interrupt_transfer(rawHandle, 0x01, frame, frame.Length, out var written, 1000);
    Console.WriteLine($"ASYNC-HYBRID write result={writeResult}, written={written}");

    if (capturedFrames.TryTake(out var reply, TimeSpan.FromSeconds(5)))
    {
        Console.WriteLine($"ASYNC-HYBRID reply captured ({reply.Length}): {Convert.ToHexString(reply)}");
    }
    else
    {
        Console.WriteLine("ASYNC-HYBRID: no reply received within 5 seconds.");
    }

    cancellation.Cancel();
    await readTask;
    device.ReleaseInterface(securityInterfaceNumber);
    device.ReleaseInterface(0);
    return 0;
}

readonly record struct GipHeader(byte Command, byte Options, byte Sequence, int PayloadLength, int ChunkValue, int HeaderLength);

/// <summary>Serializes all writes to a USB endpoint so concurrent callers cannot interleave frames.</summary>
sealed class SynchronizedUsbWriter(UsbEndpointWriter writer)
{
    private readonly object _lock = new();

    public void Write(byte[] bytes)
    {
        lock (_lock)
        {
            writer.Write(bytes, 1000, out var bytesWritten);
            if (bytesWritten != bytes.Length)
            {
                Console.WriteLine($"USB endpoint 0x01 wrote {bytesWritten} of {bytes.Length} bytes; clearing the halt and retrying.");
                writer.ClearHalt();
                writer.Write(bytes, 1000, out bytesWritten);
            }

            if (bytesWritten != bytes.Length)
            {
                throw new IOException($"USB endpoint 0x01 wrote {bytesWritten} of {bytes.Length} bytes after recovery. Reconnect the portal.");
            }

            Console.WriteLine($"TX ({bytesWritten}): {Convert.ToHexString(bytes, 0, bytesWritten)}");
        }
    }
}

sealed class GipChunkAssembly
{
    public GipChunkAssembly(byte command, int length, ReadOnlySpan<byte> firstChunk)
    {
        Command = command;
        Data = new byte[length];
        Add(0, firstChunk);
    }

    public byte Command { get; }

    public byte[] Data { get; }

    public int Received { get; private set; }

    public void Add(int offset, ReadOnlySpan<byte> chunk)
    {
        if (offset < 0 || offset + chunk.Length > Data.Length)
        {
            throw new InvalidDataException($"Chunk at offset {offset} with length {chunk.Length} exceeds transfer length {Data.Length}.");
        }

        chunk.CopyTo(Data.AsSpan(offset));
        Received = Math.Max(Received, offset + chunk.Length);
    }
}

sealed class GipProtocolState : IDisposable
{
    public ManualResetEventSlim Announced { get; } = new(false);

    public ManualResetEventSlim IdentificationCompleted { get; } = new(false);

    public ManualResetEventSlim PowerAcknowledged { get; } = new(false);

    public ManualResetEventSlim GatewayActivated { get; } = new(false);

    public bool GatewayIsActive { get; private set; }

    public void ObserveAnnounce()
    {
        Announced.Set();
    }

    public void MarkGatewayActivated()
    {
        GatewayIsActive = true;
        GatewayActivated.Set();
    }

    public void Dispose()
    {
        Announced.Dispose();
        IdentificationCompleted.Dispose();
        PowerAcknowledged.Dispose();
        GatewayActivated.Dispose();
    }
}

/// <summary>Raw P/Invoke calls into libusb-1.0.dll, bypassing LibUsbDotNet entirely, to isolate whether a bug is in that wrapper.</summary>
static class RawLibUsb
{
    public const int ErrorTimeout = -7;

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern int libusb_init(out IntPtr context);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern void libusb_exit(IntPtr context);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr libusb_open_device_with_vid_pid(IntPtr context, ushort vendorId, ushort productId);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern void libusb_close(IntPtr deviceHandle);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern int libusb_claim_interface(IntPtr deviceHandle, int interfaceNumber);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern int libusb_release_interface(IntPtr deviceHandle, int interfaceNumber);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern int libusb_interrupt_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, out int transferred, uint timeout);

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    public static extern int libusb_clear_halt(IntPtr deviceHandle, byte endpoint);
}

static class NativeLibUsb
{
    public static string GetVersion()
    {
        var versionPtr = libusb_get_version();
        var version = Marshal.PtrToStructure<LibUsbVersion>(versionPtr);
        var rc = Marshal.PtrToStringAnsi(version.Rc);
        return $"{version.Major}.{version.Minor}.{version.Micro}.{version.Nano}{rc}";
    }

    [DllImport("libusb-1.0", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_get_version();

    [StructLayout(LayoutKind.Sequential)]
    private struct LibUsbVersion
    {
        public ushort Major;
        public ushort Minor;
        public ushort Micro;
        public ushort Nano;
        public IntPtr Rc;
        public IntPtr Describe;
    }
}