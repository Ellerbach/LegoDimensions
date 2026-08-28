namespace LegoDimensions.Portal
{
    internal static class XboxGipTransport
    {
        internal const byte AnnounceCommand = 0x02;
        internal const byte AuthenticateCommand = 0x06;
        internal const byte LegoGatewayCommand = 0x21;

        internal static byte[] CreatePacket(byte command, byte options, byte sequence, ReadOnlySpan<byte> payload)
        {
            if (payload.Length > 127)
            {
                throw new ArgumentException("GIP payloads longer than 127 bytes are not supported.");
            }

            byte[] packet = new byte[4 + payload.Length];
            packet[0] = command;
            packet[1] = options;
            packet[2] = sequence;
            packet[3] = (byte)payload.Length;
            payload.CopyTo(packet.AsSpan(4));
            return packet;
        }

        internal static bool TryGetPacket(ReadOnlySpan<byte> data, out int packetLength, out byte command, out ReadOnlySpan<byte> payload)
        {
            packetLength = 0;
            command = 0;
            payload = default;
            if (data.Length < 4)
            {
                return false;
            }

            var offset = 3;
            var payloadLength = DecodeVariableLength(data, ref offset);
            if (payloadLength < 0)
            {
                return false;
            }

            if ((data[1] & 0x80) != 0 && DecodeVariableLength(data, ref offset) < 0)
            {
                return false;
            }

            packetLength = offset + payloadLength;
            if (packetLength > data.Length)
            {
                packetLength = 0;
                return false;
            }

            command = data[0];
            payload = data.Slice(offset, payloadLength);
            return true;
        }

        private static int DecodeVariableLength(ReadOnlySpan<byte> data, ref int offset)
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
    }
}