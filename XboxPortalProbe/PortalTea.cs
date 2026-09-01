using System.Buffers.Binary;

/// <summary>TEA (Tiny Encryption Algorithm) used by the portal's seed/challenge protocol (0xB1/0xB3).</summary>
internal static class PortalTea
{
    // Found in the Xbox 360 toy pad firmware (LPC11U35); shared by every portal variant.
    public static readonly byte[] SeedKey = Convert.FromHexString("55FEF63062BF0BC1C9B37C34973E29FB");

    private const uint Delta = 0x9E3779B9;
    private const int Rounds = 32;

    public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> block)
    {
        var (v0, v1, k0, k1, k2, k3) = ReadOperands(key, block);

        uint sum = 0;
        for (var i = 0; i < Rounds; i++)
        {
            sum += Delta;
            v0 += ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
            v1 += ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
        }

        return WriteResult(v0, v1);
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> block)
    {
        var (v0, v1, k0, k1, k2, k3) = ReadOperands(key, block);

        var sum = unchecked(Delta * Rounds);
        for (var i = 0; i < Rounds; i++)
        {
            v1 -= ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
            v0 -= ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
            sum -= Delta;
        }

        return WriteResult(v0, v1);
    }

    private static (uint v0, uint v1, uint k0, uint k1, uint k2, uint k3) ReadOperands(ReadOnlySpan<byte> key, ReadOnlySpan<byte> block)
    {
        if (key.Length != 16)
        {
            throw new ArgumentException("TEA key must be 16 bytes.", nameof(key));
        }

        if (block.Length != 8)
        {
            throw new ArgumentException("TEA block must be 8 bytes.", nameof(block));
        }

        return (
            BinaryPrimitives.ReadUInt32LittleEndian(block),
            BinaryPrimitives.ReadUInt32LittleEndian(block[4..]),
            BinaryPrimitives.ReadUInt32LittleEndian(key),
            BinaryPrimitives.ReadUInt32LittleEndian(key[4..]),
            BinaryPrimitives.ReadUInt32LittleEndian(key[8..]),
            BinaryPrimitives.ReadUInt32LittleEndian(key[12..]));
    }

    private static byte[] WriteResult(uint v0, uint v1)
    {
        var result = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(result, v0);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), v1);
        return result;
    }
}
