using Xunit;

namespace XboxPortalProbeTests;

public class PortalTeaTests
{
    [Theory]
    [InlineData("000102030405060708090A0B0C0D0E0F", "0011223344556677")]
    [InlineData("55FEF63062BF0BC1C9B37C34973E29FB", "0000000000000000")]
    [InlineData("55FEF63062BF0BC1C9B37C34973E29FB", "FFEEDDCCBBAA9988")]
    public void EncryptMatchesIndependentReferenceImplementation(string keyHex, string blockHex)
    {
        var key = Convert.FromHexString(keyHex);
        var block = Convert.FromHexString(blockHex);

        var expected = ReferenceEncrypt(key, block);
        var actual = PortalTea.Encrypt(key, block);

        Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(actual));
    }

    [Theory]
    [InlineData("000102030405060708090A0B0C0D0E0F", "0011223344556677")]
    [InlineData("55FEF63062BF0BC1C9B37C34973E29FB", "0000000000000000")]
    public void DecryptReversesEncrypt(string keyHex, string blockHex)
    {
        var key = Convert.FromHexString(keyHex);
        var block = Convert.FromHexString(blockHex);

        var encrypted = PortalTea.Encrypt(key, block);
        var decrypted = PortalTea.Decrypt(key, encrypted);

        Assert.Equal(Convert.ToHexString(block), Convert.ToHexString(decrypted));
    }

    [Fact]
    public void SeedKeyIsTheFirmwareConfirmedValue()
    {
        Assert.Equal("55FEF63062BF0BC1C9B37C34973E29FB", Convert.ToHexString(PortalTea.SeedKey));
    }

    // Independent transcription of the canonical TEA reference algorithm (little-endian),
    // used only to cross-check PortalTea without sharing its implementation.
    private static byte[] ReferenceEncrypt(byte[] key, byte[] block)
    {
        uint ReadU32(byte[] data, int offset) =>
            (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

        var v0 = ReadU32(block, 0);
        var v1 = ReadU32(block, 4);
        var k0 = ReadU32(key, 0);
        var k1 = ReadU32(key, 4);
        var k2 = ReadU32(key, 8);
        var k3 = ReadU32(key, 12);

        uint sum = 0;
        const uint delta = 0x9E3779B9;
        for (var i = 0; i < 32; i++)
        {
            unchecked
            {
                sum += delta;
                v0 += ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
                v1 += ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
            }
        }

        var result = new byte[8];
        void WriteU32(uint value, int offset)
        {
            result[offset] = (byte)value;
            result[offset + 1] = (byte)(value >> 8);
            result[offset + 2] = (byte)(value >> 16);
            result[offset + 3] = (byte)(value >> 24);
        }

        WriteU32(v0, 0);
        WriteU32(v1, 4);
        return result;
    }
}
