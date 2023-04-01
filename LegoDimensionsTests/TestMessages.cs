// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Text;

namespace LegoDimensionsTests
{
    public class TestMessages
    {
        [Fact]
        public void TestMessageWitStringl()
        {
            // Arrange
            Message message = new Message(MessageCommand.Wake);
            byte[] goodMessage = new byte[] { 0x55, 0x0f, 0xb0, 0x01, 0x28, 0x63, 0x29, 0x20, 0x4c, 0x45, 0x47, 0x4f, 0x20, 0x32, 0x30, 0x31, 0x34, 0xf7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            // Act
            message.AddPayload("(c) LEGO 2014");
            var bytes = message.GetBytes(1);

            // Assert
            Assert.True(bytes.SequenceEqual(goodMessage));
        }

        [Fact]
        public void TestCreateFromMessage()
        {
            // Arrange
            // Act
            var message = Message.CreateFromBuffer(new byte[] { 0x55, 0x0f, 0xb0, 0x01, 0x28, 0x63, 0x29, 0x20, 0x4c, 0x45, 0x47, 0x4f, 0x20, 0x32, 0x30, 0x31, 0x34, 0xf7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            var str = Encoding.ASCII.GetString(message.Payload);
            // Assert
            Assert.Equal(MessageType.Normal, message.MessageType);
            Assert.Equal(1, message.MessageId);
            Assert.Equal(MessageCommand.Wake, message.MessageCommand);
            Assert.Equal("(c) LEGO 2014", str);
        }
    }
}
