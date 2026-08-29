using LegoDimensions.Portal;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Buffers.Binary;

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
    Console.Error.WriteLine("Install libusb or place the matching libusb-1.0.dll beside XboxPortalProbe.exe.");
    return 2;
}

using var disposableContext = context;
if (args.Contains("probe-360", StringComparer.OrdinalIgnoreCase))
{
    return await ProbeXbox360(context);
}

if (args.Contains("describe-360", StringComparer.OrdinalIgnoreCase))
{
    var xbox360Devices = context.List()
        .Where(device => device.VendorId == Xbox360VendorId && device.ProductId == Xbox360ProductId)
        .ToArray();
    if (xbox360Devices.Length == 0)
    {
        Console.Error.WriteLine("Xbox 360 portal 24C6:FA01 was not found by libusb.");
        return 1;
    }

    foreach (var xbox360Device in xbox360Devices)
    {
        Console.WriteLine($"Device {xbox360Device.VendorId:X4}:{xbox360Device.ProductId:X4}");
        foreach (var xbox360Configuration in xbox360Device.Configs)
        {
            DumpDescriptor("  Configuration", xbox360Configuration);
            foreach (var xbox360Interface in xbox360Configuration.Interfaces)
            {
                DumpDescriptor("    Interface", xbox360Interface);
                foreach (var xbox360Endpoint in xbox360Interface.Endpoints)
                {
                    DumpDescriptor("      Endpoint", xbox360Endpoint);
                }
            }
        }
    }

    return 0;
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
        return await ProbeXbox360(context);
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

static async Task<int> ProbeXbox360(UsbContext context)
{
    var devices = context.List()
        .Where(device => device.VendorId == Xbox360VendorId && device.ProductId == Xbox360ProductId)
        .ToArray();
    if (devices.Length == 0)
    {
        Console.Error.WriteLine("Xbox 360 portal 24C6:FA01 was not found by libusb.");
        return 1;
    }

    using var device = devices[0];
    try
    {
        device.Open();
    }
    catch (UsbException exception)
    {
        Console.Error.WriteLine($"Xbox 360 portal 24C6:FA01 could not be opened: {exception.Message}");
        return 3;
    }

    var configuration = device.Configs[0];
    try
    {
        device.SetConfiguration(configuration.ConfigurationValue);
    }
    catch (UsbException exception)
    {
        Console.WriteLine($"USB configuration {configuration.ConfigurationValue} could not be selected; continuing: {exception.Message}");
    }

    var interfaceNumber = configuration.Interfaces[0].Number;
    var securityInterfaceNumber = configuration.Interfaces.Single(interfaceInfo => interfaceInfo.Number == 3).Number;
    device.ClaimInterface(interfaceNumber);
    var reader = device.OpenEndpointReader(ReadEndpointID.Ep01, UsbEndpointReader.DefReadBufferSize, EndpointType.Interrupt);
    var writer = new SynchronizedUsbWriter(device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Interrupt));
    device.ClaimInterface(securityInterfaceNumber);
    using var cancellation = new CancellationTokenSource();
    using var controlTransferGate = new SemaphoreSlim(1, 1);
    using var interruptReadsEnabled = new ManualResetEventSlim(true);
    var readTask = Task.Run(() => ReadRawPackets(reader, controlTransferGate, interruptReadsEnabled, cancellation.Token));
    byte messageId = 0;

    Console.WriteLine($"Opened Xbox 360 portal 24C6:FA01, interface {interfaceNumber}, endpoints 81/01.");
    Console.WriteLine("Listening for raw input. Type 'help' for commands.");

    try
    {
        while (true)
        {
            Console.Write("360> ");
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
                        Console.WriteLine("send <hex>                              Send exact bytes to endpoint 01");
                        Console.WriteLine("wake                                    Send an Xbox 360-wrapped LEGO wake frame");
                        Console.WriteLine("xinput-led                              Send Xbox 360 LED command 01-03-06");
                        Console.WriteLine("xsm3-auth                               Complete Xbox 360 security authentication");
                        Console.WriteLine("control-in <type request value index n> Issue a vendor control-IN request; numbers are hex");
                        Console.WriteLine("control-out <type request value index hex> Issue a vendor control-OUT request; numbers are hex");
                        Console.WriteLine("quit                                    Close the device and exit");
                        break;
                    case "send":
                        Send(writer, ParseHex(parts.ElementAtOrDefault(1)));
                        break;
                    case "wake":
                        var wake = new Message(MessageCommand.Wake);
                        wake.AddPayload("(c) LEGO 2014");
                        var wakeFrame = wake.GetBytes(NextMessageId(ref messageId));
                        Send(writer, Xbox360Transport.WrapLegoFrame(wakeFrame));
                        break;
                    case "xinput-led":
                        Send(writer, [0x01, 0x03, 0x06]);
                        break;
                    case "xsm3-auth":
                        AuthenticateXbox360(device, controlTransferGate, interruptReadsEnabled);
                        break;
                    case "control-in":
                        SendControlIn(device, controlTransferGate, interruptReadsEnabled, parts.ElementAtOrDefault(1));
                        break;
                    case "control-out":
                        SendControlOut(device, controlTransferGate, interruptReadsEnabled, parts.ElementAtOrDefault(1));
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
        device.ReleaseInterface(securityInterfaceNumber);
        device.ReleaseInterface(interfaceNumber);
        device.Close();
    }

    return 0;
}

static void ReadRawPackets(UsbEndpointReader reader, SemaphoreSlim controlTransferGate, ManualResetEventSlim interruptReadsEnabled, CancellationToken cancellationToken)
{
    var buffer = new byte[1024];
    var consecutiveNotFoundErrors = 0;
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            interruptReadsEnabled.Wait(cancellationToken);
            controlTransferGate.Wait(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // quit cancelled the token while we were waiting; let the caller's cleanup run.
            break;
        }

        Error error;
        int bytesRead;
        try
        {
            error = reader.Read(buffer, TransferTimeout, out bytesRead);
        }
        finally
        {
            controlTransferGate.Release();
        }

        if (error == Error.Timeout)
        {
            consecutiveNotFoundErrors = 0;
            continue;
        }

        Console.WriteLine(error == Error.Success
            ? $"\nRX ({bytesRead}): {Convert.ToHexString(buffer, 0, bytesRead)}"
            : $"\nRX error: {error}, bytes={bytesRead}");
        if (error == Error.NotFound && ++consecutiveNotFoundErrors < 3)
        {
            continue;
        }

        if (error != Error.Success)
        {
            break;
        }

        consecutiveNotFoundErrors = 0;
    }
}

static void SendControlIn(IUsbDevice device, SemaphoreSlim controlTransferGate, ManualResetEventSlim interruptReadsEnabled, string? arguments)
{
    var fields = arguments?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length != 5 || fields.Any(field => !int.TryParse(field, System.Globalization.NumberStyles.HexNumber, null, out _)))
    {
        throw new ArgumentException("Usage: control-in <request-type request value index length>; all numbers are hex");
    }

    var values = fields.Select(field => Convert.ToInt32(field, 16)).ToArray();
    var buffer = ControlIn(device, controlTransferGate, interruptReadsEnabled,
        (byte)values[0], (byte)values[1], values[2], values[3], values[4]);

    Console.WriteLine($"CONTROL RX ({buffer.Length}): {Convert.ToHexString(buffer)}");
}

static void SendControlOut(IUsbDevice device, SemaphoreSlim controlTransferGate, ManualResetEventSlim interruptReadsEnabled, string? arguments)
{
    var fields = arguments?.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (fields.Length != 5 || fields.Take(4).Any(field => !int.TryParse(field, System.Globalization.NumberStyles.HexNumber, null, out _)))
    {
        throw new ArgumentException("Usage: control-out <request-type request value index hex>; all numbers are hex");
    }

    var values = fields.Take(4).Select(field => Convert.ToInt32(field, 16)).ToArray();
    var buffer = ParseHex(fields[4]);
    ControlOut(device, controlTransferGate, interruptReadsEnabled,
        (byte)values[0], (byte)values[1], values[2], values[3], buffer);

    Console.WriteLine($"CONTROL TX ({buffer.Length}): {Convert.ToHexString(buffer)}");
}

static void AuthenticateXbox360(IUsbDevice device, SemaphoreSlim controlTransferGate, ManualResetEventSlim interruptReadsEnabled)
{
    var challenge = Convert.FromHexString(
        "094000001CDEEB918766B0E3C0B26C056DC867E2E7D6A5DC716F211FB43228A0C289");

    var identity = ControlIn(device, controlTransferGate, interruptReadsEnabled, 0xC1, 0x81, 0x5B17, 0x0103, 0x1D);
    Console.WriteLine($"XSM3 81 RX: {Convert.ToHexString(identity)}");

    ControlOut(device, controlTransferGate, interruptReadsEnabled, 0x41, 0x82, 0x0003, 0x0103, challenge);
    Console.WriteLine($"XSM3 82 TX: {Convert.ToHexString(challenge)}");
    WaitForXbox360SecurityResponse(device, controlTransferGate, interruptReadsEnabled);

    var challengeResponse = ControlIn(device, controlTransferGate, interruptReadsEnabled, 0xC1, 0x83, 0x5C28, 0x0103, 0x2E);
    Console.WriteLine($"XSM3 83 RX: {Convert.ToHexString(challengeResponse)}");
    var session = Xbox360Xsm3Host.Create(identity, challenge, challengeResponse);

    var verify = session.CreateVerifyPacket();
    ControlOut(device, controlTransferGate, interruptReadsEnabled, 0x41, 0x87, 0x0003, 0x0103, verify);
    Console.WriteLine($"XSM3 87 TX: {Convert.ToHexString(verify)}");
    WaitForXbox360SecurityResponse(device, controlTransferGate, interruptReadsEnabled);

    var verifyResponse = ControlIn(device, controlTransferGate, interruptReadsEnabled, 0xC1, 0x83, 0x5C10, 0x0103, 0x16);
    Console.WriteLine($"XSM3 83 RX: {Convert.ToHexString(verifyResponse)}");
    session.ValidateFinalResponse(verifyResponse);

    _ = ControlIn(device, controlTransferGate, interruptReadsEnabled, 0xC1, 0x84, 0x0003, 0x0103, 0);
    Console.WriteLine("XSM3 authentication completed and verified.");
}

static void WaitForXbox360SecurityResponse(IUsbDevice device, SemaphoreSlim controlTransferGate, ManualResetEventSlim interruptReadsEnabled)
{
    var deadline = Environment.TickCount64 + 2000;
    while (Environment.TickCount64 < deadline)
    {
        var status = ControlIn(device, controlTransferGate, interruptReadsEnabled, 0xC1, 0x86, 0x0000, 0x0103, 2);
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
    SemaphoreSlim controlTransferGate,
    ManualResetEventSlim interruptReadsEnabled,
    byte requestType,
    byte request,
    int value,
    int index,
    int length)
{
    var buffer = new byte[length];
    ControlTransfer(device, controlTransferGate, interruptReadsEnabled,
        new UsbSetupPacket(requestType, request, value, index, length), buffer);
    return buffer;
}

static void ControlOut(
    IUsbDevice device,
    SemaphoreSlim controlTransferGate,
    ManualResetEventSlim interruptReadsEnabled,
    byte requestType,
    byte request,
    int value,
    int index,
    byte[] buffer)
{
    ControlTransfer(device, controlTransferGate, interruptReadsEnabled,
        new UsbSetupPacket(requestType, request, value, index, buffer.Length), buffer);
}

static void ControlTransfer(
    IUsbDevice device,
    SemaphoreSlim controlTransferGate,
    ManualResetEventSlim interruptReadsEnabled,
    UsbSetupPacket setupPacket,
    byte[] buffer)
{
    interruptReadsEnabled.Reset();
    try
    {
        controlTransferGate.Wait();
        try
        {
            device.ControlTransfer(setupPacket, buffer, 0, buffer.Length);
        }
        finally
        {
            controlTransferGate.Release();
        }
    }
    finally
    {
        interruptReadsEnabled.Set();
    }
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