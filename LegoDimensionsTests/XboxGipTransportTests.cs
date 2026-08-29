namespace LegoDimensionsTests
{
    public class XboxGipTransportTests
    {
        [Fact]
        public void CreatePacket_WrapsLegoMessage()
        {
            // Arrange
            var message = new Message(MessageCommand.Color);
            message.AddPayload(new byte[] { (byte)Pad.Center, 0xFF, 0x00, 0x00 });

            // Act
            var packet = XboxGipTransport.CreatePacket(0x21, 0x00, 0x02, message.GetBytes(0x02));

            // Assert
            Assert.Equal(36, packet.Length);
            Assert.Equal("210002205506C00201FF00001D0000000000000000000000000000000000000000000000", Convert.ToHexString(packet));
        }

        [Fact]
        public void TryGetPacket_ExtractsLegoResponse()
        {
            // Arrange
            var packet = Convert.FromHexString("2100232055120600CEC43EF638A9323700000000000000007D0000000000000000000000");

            // Act
            var parsed = XboxGipTransport.TryGetPacket(packet, out var packetLength, out var command, out var payload);
            var message = Message.CreateFromBuffer(payload.ToArray(), MessageSource.Portal);

            // Assert
            Assert.True(parsed);
            Assert.Equal(36, packetLength);
            Assert.Equal(0x21, command);
            Assert.Equal(6, message.MessageId);
            Assert.Equal("00CEC43EF638A932370000000000000000", Convert.ToHexString(message.Payload));
        }

        [Fact]
        public void TryGetPacket_ReturnsFirstOfConcatenatedPackets()
        {
            // Arrange
            byte[] first = [0x02, 0x20, 0x01, 0x02, 0xAA, 0xBB];
            byte[] second = [0x06, 0x20, 0x02, 0x02, 0x01, 0x00];
            byte[] packets = [.. first, .. second];

            // Act
            var parsed = XboxGipTransport.TryGetPacket(packets, out var packetLength, out var command, out var payload);

            // Assert
            Assert.True(parsed);
            Assert.Equal(first.Length, packetLength);
            Assert.Equal(0x02, command);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, payload.ToArray());
        }

        [Fact]
        public void TryGetPacket_ParsesChunkMetadata()
        {
            // Arrange
            byte[] packet = [0x04, 0xF0, 0x01, 0x02, 0x08, 0xAA, 0xBB];

            // Act
            var parsed = XboxGipTransport.TryGetPacket(packet, out var packetLength, out var command, out var payload);

            // Assert
            Assert.True(parsed);
            Assert.Equal(packet.Length, packetLength);
            Assert.Equal(0x04, command);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, payload.ToArray());
        }

        [Fact]
        public void TryGetPacket_RejectsTruncatedPayload()
        {
            // Arrange
            byte[] packet = [0x21, 0x00, 0x01, 0x20, 0x55];

            // Act
            var parsed = XboxGipTransport.TryGetPacket(packet, out var packetLength, out _, out _);

            // Assert
            Assert.False(parsed);
            Assert.Equal(0, packetLength);
        }
    }
}