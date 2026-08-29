using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

internal sealed class Xbox360Xsm3Host
{
    private static readonly byte[] FixedKey1D =
    [
        0xE3, 0x5B, 0xFB, 0x1C, 0xCD, 0xAD, 0x32, 0x5B,
        0xF7, 0x0E, 0x07, 0xFD, 0x62, 0x3D, 0xA7, 0xC4
    ];

    private static readonly byte[] FixedKey1E =
    [
        0x8F, 0x29, 0x08, 0x38, 0x0B, 0x5B, 0xFE, 0x68,
        0x7C, 0x26, 0x46, 0x2A, 0x51, 0xF2, 0xBC, 0x19
    ];

    private static readonly byte[] KeyVaultKey1 =
    [
        0xF1, 0x9D, 0x6F, 0x2C, 0xB1, 0xEE, 0x6A, 0xC4,
        0x63, 0x53, 0x36, 0xA5, 0x4C, 0x11, 0x00, 0x7D
    ];

    private static readonly byte[] KeyVaultKey2 =
    [
        0xC4, 0x55, 0x82, 0xC8, 0x9F, 0xC3, 0xDA, 0xD2,
        0x8C, 0x1F, 0xBB, 0xCF, 0x3D, 0x04, 0x9B, 0x6F
    ];

    private static readonly byte[] SBox = Convert.FromHexString(
        "B03D9B70F3C78060739F6CC0F13DBB40B3C83714DF49DAD4482278806ECDE700" +
        "818668E15D7C542C557BEF48427B3B68E3DBAAC00FA99620950593949AF6A364" +
        "5DCC7600E50819E88D29D74C219117F4BC6AB38083C6D4909BAE0EFE2E4AF200" +
        "7388D94066C5D40857B18948DC54FC436A2687B8095FCE80E40B059C24F3DEE2" +
        "3EEC388AA255A4504E4BE9587F9F7D80230C4D80054426B8E9D8BCE6763A6EA4" +
        "19DEC2D0C4BCC35C59DF16463970F4EE2D585AA817866B6029584DD25F287AD8" +
        "8E79EA8294333181D922D510DA92A07D3DDAAC1CA25331B83C965200826B56A0" +
        "D3C240C71B7FDC017270B18C01090936FC97EADEE30DAE7EE30DAE7E33698040");

    private static readonly byte[] AcrPlaintext = Convert.FromHexString(
        "D1D2F2806EBA0CC0B6C4C9D861751D1A3F9558BED80DE2C0D0217920652D9940" +
        "3C9652001B7FDC01821C13D833698040FC97EADE08EA14DCEB0F6A186F782CB0" +
        "D3C240C7826B56A0190936E07270B18CE30DAE7E50A52BE2C9AFC7701C298056" +
        "24F066FA022B58988FE4D13C6E382AFFB8FA35B05249C5B466FA47556C8D4008");

    private readonly byte[] _consoleRandom;
    private readonly byte[] _usbRandom;
    private readonly byte[] _randomEncrypted;
    private readonly byte[] _randomSwapEncrypted;
    private readonly byte[] _commandHash;
    private readonly byte[] _protocolData;
    private readonly byte[] _consoleId;
    private byte[]? _finalResponseSalt;

    private Xbox360Xsm3Host(
        byte[] consoleRandom,
        byte[] usbRandom,
        byte[] randomEncrypted,
        byte[] randomSwapEncrypted,
        byte[] commandHash,
        byte[] protocolData,
        byte[] consoleId)
    {
        _consoleRandom = consoleRandom;
        _usbRandom = usbRandom;
        _randomEncrypted = randomEncrypted;
        _randomSwapEncrypted = randomSwapEncrypted;
        _commandHash = commandHash;
        _protocolData = protocolData;
        _consoleId = consoleId;
    }

    public static Xbox360Xsm3Host Create(
        ReadOnlySpan<byte> identity,
        ReadOnlySpan<byte> challenge,
        ReadOnlySpan<byte> response)
    {
        ValidatePacket(identity, 0x49, 0x4B, 0x17);
        ValidatePacket(challenge, 0x09, 0x40, 0x1C);
        ValidatePacket(response, 0x49, 0x4C, 0x28);

        var challengePlaintext = AuthenticationCrypt(FixedKey1D, challenge.Slice(5, 24), false);
        var consoleRandom = challengePlaintext[..16];
        var consoleId = challengePlaintext[16..24];

        var challengeMac = AuthenticationMac(FixedKey1E, null, challenge.Slice(5, 24));
        if (!CryptographicOperations.FixedTimeEquals(challengeMac.AsSpan(4, 4), challenge.Slice(29, 4)))
        {
            throw new CryptographicException("XSM3 challenge MAC is invalid.");
        }

        var randomEncrypted = AuthenticationCrypt(KeyVaultKey1, consoleRandom, true);

        var randomSwap = new byte[16];
        consoleRandom.AsSpan(8, 8).CopyTo(randomSwap);
        consoleRandom.AsSpan(0, 8).CopyTo(randomSwap.AsSpan(8));
        var randomSwapEncrypted = AuthenticationCrypt(KeyVaultKey2, randomSwap, true);

        var responsePlaintext = AuthenticationCrypt(randomEncrypted, response.Slice(5, 32), false);
        var usbRandom = responsePlaintext[..16];
        if (!CryptographicOperations.FixedTimeEquals(consoleRandom, responsePlaintext.AsSpan(16, 16)))
        {
            throw new CryptographicException(
                "XSM3 phase-one response could not be decrypted with the configured 0x23 key; this device requires different 0x23/0x24 key material.");
        }

        var protocolData = CreateProtocolData(identity);
        var responseMac = AuthenticationMac(randomSwapEncrypted, null, response.Slice(5, 32));
        var responseAcr = AuthenticationAcr(protocolData, consoleId, responseMac);
        if (!CryptographicOperations.FixedTimeEquals(responseAcr, response.Slice(37, 8)))
        {
            throw new CryptographicException("XSM3 response ACR is invalid.");
        }

        return new Xbox360Xsm3Host(
            consoleRandom,
            usbRandom,
            randomEncrypted,
            randomSwapEncrypted,
            SHA1.HashData(responsePlaintext),
            protocolData,
            consoleId);
    }

    public byte[] CreateVerifyPacket()
    {
        Span<byte> plaintext = stackalloc byte[8];
        RandomNumberGenerator.Fill(plaintext);
        return CreateVerifyPacket(plaintext);
    }

    internal byte[] CreateVerifyPacket(ReadOnlySpan<byte> plaintext)
    {
        if (plaintext.Length != 8)
        {
            throw new ArgumentException("XSM3 verify plaintext must be 8 bytes.", nameof(plaintext));
        }

        var encrypted = AuthenticationCrypt(_usbRandom, plaintext, true);
        var salt = CreateVerifySalt();
        plaintext.CopyTo(salt.AsSpan(8));
        var mac = AuthenticationMac(_commandHash, salt, encrypted);

        var packet = new byte[22];
        packet[0] = 0x09;
        packet[1] = 0x41;
        packet[4] = 0x10;
        encrypted.CopyTo(packet, 5);
        mac.CopyTo(packet, 13);
        packet[21] = CalculateChecksum(packet);
        _finalResponseSalt = salt;
        return packet;
    }

    internal void AcceptVerifyPacket(ReadOnlySpan<byte> packet)
    {
        ValidatePacket(packet, 0x09, 0x41, 0x10);
        var plaintext = AuthenticationCrypt(_usbRandom, packet.Slice(5, 8), false);
        var salt = CreateVerifySalt();
        plaintext.CopyTo(salt.AsSpan(8));
        var expectedMac = AuthenticationMac(_commandHash, salt, packet.Slice(5, 8));
        if (!CryptographicOperations.FixedTimeEquals(expectedMac, packet.Slice(13, 8)))
        {
            throw new CryptographicException("XSM3 verify packet MAC is invalid.");
        }

        _finalResponseSalt = salt;
    }

    public void ValidateFinalResponse(ReadOnlySpan<byte> response)
    {
        if (_finalResponseSalt is null)
        {
            throw new InvalidOperationException("An XSM3 verify packet has not been generated.");
        }

        ValidatePacket(response, 0x49, 0x4C, 0x10);
        var salt = (byte[])_finalResponseSalt.Clone();
        var expectedMac = AuthenticationMac(_randomSwapEncrypted, salt, response.Slice(5, 8));
        if (!CryptographicOperations.FixedTimeEquals(expectedMac, response.Slice(13, 8)))
        {
            throw new CryptographicException("XSM3 final response MAC is invalid.");
        }

        var responseAcr = AuthenticationCrypt(_randomEncrypted, response.Slice(5, 8), false);
        var expectedAcr = AuthenticationAcr(_protocolData, _consoleId, salt.AsSpan(8, 8));
        if (!CryptographicOperations.FixedTimeEquals(expectedAcr, responseAcr))
        {
            throw new CryptographicException("XSM3 final response ACR is invalid.");
        }
    }

    private byte[] CreateVerifySalt()
    {
        var salt = (byte[])_consoleRandom.Clone();
        _usbRandom.AsSpan(12, 4).CopyTo(salt);
        _consoleRandom.AsSpan(12, 4).CopyTo(salt.AsSpan(4));
        return salt;
    }

    private static byte[] AuthenticationCrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, bool encrypt)
    {
        using var tripleDes = TripleDES.Create();
        tripleDes.Mode = CipherMode.CBC;
        tripleDes.Padding = PaddingMode.None;
        tripleDes.Key = SetOddParity(key);
        tripleDes.IV = new byte[8];
        using var transform = encrypt ? tripleDes.CreateEncryptor() : tripleDes.CreateDecryptor();
        return transform.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }

    private static byte[] AuthenticationMac(ReadOnlySpan<byte> key, byte[]? salt, ReadOnlySpan<byte> input)
    {
        if (input.Length == 0 || input.Length % 8 != 0)
        {
            throw new ArgumentException("XSM3 MAC input must contain complete 8-byte blocks.", nameof(input));
        }

        var parityKey = SetOddParity(key[..16]);
        var temporary = new byte[8];
        if (salt is not null)
        {
            IncrementBigEndian(salt.AsSpan(0, 8));
            temporary = TransformDes(parityKey.AsSpan(0, 8), salt.AsSpan(0, 8), true);
        }

        for (var offset = 0; offset < input.Length; offset += 8)
        {
            for (var index = 0; index < 8; index++)
            {
                temporary[index] ^= input[offset + index];
            }

            temporary = TransformDes(parityKey.AsSpan(0, 8), temporary, true);
        }

        temporary[0] ^= 0x80;
        return AuthenticationCrypt(parityKey, temporary, true);
    }

    private static byte[] AuthenticationAcr(ReadOnlySpan<byte> protocolData, ReadOnlySpan<byte> consoleId, ReadOnlySpan<byte> key)
    {
        var block = new byte[8];
        protocolData[..4].CopyTo(block);
        consoleId[..4].CopyTo(block.AsSpan(4));
        var iv = ParveEcb(key, protocolData.Slice(16, 8));
        var cd = ParveEcb(key, block);
        var ab = iv;
        for (var offset = 0; offset < AcrPlaintext.Length; offset += 8)
        {
            for (var index = 0; index < 8; index++)
            {
                ab[index] ^= AcrPlaintext[offset + index];
            }

            ab = ParveEcb(key, ab);
        }

        const ulong modulus = 0x7FFFFFFF;
        var ab0 = BinaryPrimitives.ReadUInt32BigEndian(ab) % modulus;
        var ab1 = BinaryPrimitives.ReadUInt32BigEndian(ab.AsSpan(4)) % modulus;
        var cd0 = BinaryPrimitives.ReadUInt32BigEndian(cd) % modulus;
        var cd1 = BinaryPrimitives.ReadUInt32BigEndian(cd.AsSpan(4)) % modulus;
        ulong out0 = 0;
        ulong out1 = 0;
        for (var offset = 0; offset < AcrPlaintext.Length; offset += 8)
        {
            var temporary = (BinaryPrimitives.ReadUInt32BigEndian(AcrPlaintext.AsSpan(offset)) * 0xE79A9C1UL + out0) % modulus;
            temporary = (temporary * ab0 + ab1) % modulus;
            out1 += temporary;
            temporary = ((BinaryPrimitives.ReadUInt32BigEndian(AcrPlaintext.AsSpan(offset + 4)) + temporary) * cd0) % modulus;
            out0 = (temporary + cd1) % modulus;
            out1 += out0;
        }

        var result = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(result, (uint)((out0 + ab1) % modulus));
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), (uint)((out1 + cd1) % modulus));
        for (var index = 0; index < result.Length; index++)
        {
            result[index] ^= ab[index];
        }

        return result;
    }

    private static byte[] ParveEcb(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input)
    {
        var block = new byte[9];
        input[..8].CopyTo(block);
        block[8] = block[0];
        for (var round = 8; round > 0; round--)
        {
            for (var index = 0; index < 8; index++)
            {
                var x = (byte)(key[index] + block[index] + round);
                var y = (byte)(SBox[x] + block[index + 1]);
                block[index + 1] = (byte)((y << 1) | (y >> 7));
            }

            block[0] = block[8];
        }

        return block[..8];
    }

    private static byte[] CreateProtocolData(ReadOnlySpan<byte> identity)
    {
        var payload = identity[5..];
        var result = new byte[32];
        payload[..15].CopyTo(result);
        payload.Slice(15, 5).CopyTo(result.AsSpan(16));
        result[21] = payload[22];
        payload.Slice(20, 2).CopyTo(result.AsSpan(22));
        return result;
    }

    private static byte[] TransformDes(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, bool encrypt)
    {
        using var des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.None;
        des.Key = key.ToArray();
        using var transform = encrypt ? des.CreateEncryptor() : des.CreateDecryptor();
        return transform.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }

    private static byte[] SetOddParity(ReadOnlySpan<byte> key)
    {
        var result = key.ToArray();
        for (var index = 0; index < result.Length; index++)
        {
            result[index] &= 0xFE;
            if ((BitOperations.PopCount(result[index]) & 1) == 0)
            {
                result[index] |= 0x01;
            }
        }

        return result;
    }

    private static void IncrementBigEndian(Span<byte> value)
    {
        for (var index = value.Length - 1; index >= 0; index--)
        {
            if (++value[index] != 0)
            {
                return;
            }
        }
    }

    private static void ValidatePacket(ReadOnlySpan<byte> packet, byte first, byte second, byte payloadLength)
    {
        if (packet.Length != payloadLength + 6 || packet[0] != first || packet[1] != second || packet[4] != payloadLength)
        {
            throw new InvalidDataException("Unexpected XSM3 packet header or length.");
        }

        if (CalculateChecksum(packet) != packet[^1])
        {
            throw new InvalidDataException("XSM3 packet checksum is invalid.");
        }
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> packet)
    {
        var checksum = (byte)0;
        for (var index = 5; index < packet.Length - 1; index++)
        {
            checksum ^= packet[index];
        }

        return checksum;
    }
}