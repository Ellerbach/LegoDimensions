// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensionsTests
{
    public class TestDecryption
    {
        [Fact]
        public void DecryptVehicle()
        {
            // Arrange
            byte[] data = new byte[] { 0x63, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            // Act & Assert
            Assert.True(LegoTag.IsVehicle(data.AsSpan(8, 4).ToArray()));
            var id = LegoTag.GetVehiculeId(data);
            Assert.Equal(123, id);
        }

        [Fact]
        public void DecryptCharacter()
        {
            // Arrange
            byte[] data = new byte[] { 0x5C, 0xF7, 0x1C, 0xDE, 0x29, 0xAD, 0xEA, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] uid = new byte[] { 0x04, 0x47, 0x37, 0xE2, 0x48, 0x3F, 0x80 };

            // Act & Assert
            Assert.False(LegoTag.IsVehicle(data.AsSpan(8, 4).ToArray()));
            var id = LegoTag.GetCharacterId(uid, data.AsSpan(0, 8).ToArray());
            Assert.Equal(16, id);
        }

        [Fact]
        public void EncryptCharacter()
        {
            // Arrange
            byte[] data;
            byte[] correctData = new byte[] { 0x5C, 0xF7, 0x1C, 0xDE, 0x29, 0xAD, 0xEA, 0x08 };
            byte[] uid = new byte[] { 0x04, 0x47, 0x37, 0xE2, 0x48, 0x3F, 0x80 };

            // Act
            data = LegoTag.EncrypCharactertId(uid, 16);
            var id = LegoTag.GetCharacterId(uid, data);

            // Assert
            Assert.True(data.SequenceEqual(correctData));
            Assert.Equal(16, id);
        }

        [Fact]
        public void EncryptVehicle()
        {
            // Arrange
            ushort id = 123;
            byte[] data;

            // Act
            data = LegoTag.EncryptVehicleId(id);

            // Assert
            Assert.True(data.SequenceEqual(new byte[] { 0x63, 0x04, 0x00, 0x00 }));
        }

        [Fact]
        public void TestPasswordGeneration()
        {
            // Arrange
            byte[] uid = new byte[] { 0x04, 0x27, 0x50, 0x4A, 0x50, 0x49, 0x80 };
            byte[] correctPassword = new byte[] { 0xA1, 0x7B, 0x4C, 0x95 };

            // Act
            var data = LegoTag.GenerateCardPassword(uid);

            Assert.True(data.SequenceEqual(correctPassword));
        }
    }
}