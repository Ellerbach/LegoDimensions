// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace LegoDimensions
{
    /// <summary>
    /// Manage Lego Tag from NFC data.
    /// </summary>
    public static class LegoTag
    {
        /// <summary>
        /// Generates the password to read the NFC card.
        /// </summary>
        /// <param name="uid">The 7 bytes RFID serial number.</param>
        /// <returns>A 4 bytes array password to use to authenticate.</returns>
        /// <exception cref="ArgumentException">UID must be 7 bytes long.</exception>
        public static byte[] GenerateCardPassword(byte[] uid)
        {
            if (uid is null || (uid.Length is not 7))
            {
                throw new ArgumentException("UID must be 7 bytes long");
            }

            uint b;
            uint v2 = 0;

            // "UUUUUUU(c) Copyright LEGO 2014AA"
            byte[] basic = {
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x28,
                0x63, 0x29, 0x20, 0x43, 0x6f, 0x70, 0x79, 0x72,
                0x69, 0x67, 0x68, 0x74, 0x20, 0x4c, 0x45, 0x47,
                0x4f, 0x20, 0x32, 0x30, 0x31, 0x34, 0xaa, 0xaa
            };

            Array.Copy(uid, basic, uid.Length);
            for (int i = 0; i < 8; i++)
            {
                uint v4 = RotateRight(v2, 25);
                uint v5 = RotateRight(v2, 10);
                b = (uint)(basic[i * 4 + 3] << 24 |
                              basic[i * 4 + 2] << 16 |
                              basic[i * 4 + 1] << 8 |
                              basic[i * 4]);
                v2 = (b + v4 + v5 - v2);
            }

            return BitConverter.GetBytes(v2);
        }

        /// <summary>
        /// Gets the vehicle ID by decrypting the data.
        /// </summary>
        /// <param name="data">Page 0x24 of the NFC data.</param>
        /// <returns>The vehicle ID.</returns>
        /// <exception cref="ArgumentException">Data must be at least 2 bytes long</exception>
        public static ushort GetVehicleId(byte[] data)
        {
            if (data is null || data.Length < 2)
            {
                throw new ArgumentException("Data must be at least 2 bytes long");
            }

            return (ushort)(data[0] | (data[1] << 8));
        }

        /// <summary>
        /// Checks if the tag is a vehicles.
        /// </summary>
        /// <param name="data">Page 0x26 of the NFC data.</param>
        /// <returns>True if the tag is a vehicle.</returns>
        public static bool IsVehicle(byte[] data)
        {
            return data.SequenceEqual(new byte[] { 0x00, 0x01, 0x00, 0x00 });
        }

        /// <summary>
        /// Gets the character ID by decrypting the data.
        /// </summary>
        /// <param name="uid">The 7 bytes RFID serial number.</param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">UID must be 7 bytes long.</exception>
        /// <exception cref="ArgumentException">Data must be at least 8 bytes long.</exception>
        public static ushort GetCharacterId(byte[] uid, byte[] data)
        {
            if (uid is null || (uid.Length is not 7))
            {
                throw new ArgumentException("UID must be 7 bytes long.");
            }

            if (data is null || data.Length < 8)
            {
                throw new ArgumentException("Data must be at least 8 bytes long.");
            }

            uint[] key = new uint[4];
            uint[] data32 = new uint[2];

            // TODO: clean Copy data into data32
            data32[0] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));
            data32[1] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4, 4));

            Generatekeys(uid, key);
            TeaDecrypt(data32, key);
            if (data32[0] != data32[1])
            {
                Debug.WriteLine("Invalid ID, values not identical.");
                return 0;
            }
            //flipBytes(v2);
            return (ushort)data32[0];
        }

        /// <summary>
        /// Encrypt a character ID.
        /// </summary>
        /// <param name="uid"The 7 bytes RFID serial number.></param>
        /// <param name="charid">The character ID.</param>
        /// <returns>A 8 bytes encrypted data to be stored in the 0x24 and 0x25 NFC page.</returns>
        /// <exception cref="ArgumentException">UID must be 7 bytes long.</exception>
        public static byte[] EncrypCharactertId(byte[] uid, ushort charid)
        {
            if (uid is null || (uid.Length is not 7))
            {
                throw new ArgumentException("UID must be 7 bytes long.");
            }

            uint[] key = new uint[4];
            uint[] buf = new uint[2];
            byte[] data = new byte[8];

            Generatekeys(uid, key);
            buf[0] = charid;
            buf[1] = charid;
            TeaEncrypt(buf, key);
            for (int i = 0; i < 4; i++)
            {
                data[i] = (byte)(buf[0] >> (i * 8));
                data[i + 4] = (byte)(buf[1] >> (i * 8));
            }

            return data;
        }

        /// <summary>
        /// Encrypt the vehicle ID.
        /// </summary>
        /// <param name="vecId">The vehicle ID.</param>
        /// <returns>A 4 bytes array to be stored at page 0x24 of the NFC. Note that page 0x26 needs to be set with 0x00 0x01 0x00 0x00.</returns>
        public static byte[] EncryptVehicleId(ushort vecId)
        {
            byte[] data = new byte[4] { 0x00, 0x00, 0x00, 0x00 };            
            data[0] = (byte)(vecId & 0xFF);
            data[1] = (byte)((vecId >> 8) & 0xFF);

            return data;
        }

        private static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        private static uint Scramble(byte[] uid, byte cnt)
        {
            byte[] basic = new byte[24] {
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xb7,
                0xd5, 0xd7, 0xe6, 0xe7,
                0xba, 0x3c, 0xa8, 0xd8,
                0x75, 0x47, 0x68, 0xcf,
                0x23, 0xe9, 0xfe, 0xaa };
            uint v2 = 0, b = 0;

            // Copy the UID into the first 7 bytes of the basic array
            Array.Copy(uid, basic, uid.Length);

            basic[(cnt * 4) - 1] = 0xaa;
            for (int i = 0; i < cnt; i++)
            {
                b = BinaryPrimitives.ReadUInt32LittleEndian(basic.AsSpan(i * 4, 4));
                v2 = b + RotateRight(v2, 25) + RotateRight(v2, 10) - v2;
            }

            return v2;
        }

        private static void Generatekeys(byte[] uid, uint[] key)
        {
            key[0] = Scramble(uid, 3);
            key[1] = Scramble(uid, 4);
            key[2] = Scramble(uid, 5);
            key[3] = Scramble(uid, 6);
        }

        /// <summary>
        /// Encrypt 64 bits with a 128 bit key using TEA.
        /// From http://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm
        /// </summary>
        /// <param name="v">Array of two 32 bit uints to be encoded in place.</param>
        /// <param name="k">Array of four 32 bit uints to act as key.</param>
        private static void TeaEncrypt(uint[] v, uint[] k)
        {
            /* set up */
            uint v0 = v[0];
            uint v1 = v[1];
            uint sum = 0;
            uint i;

            /* a key schedule constant */
            uint delta = 0x9e3779b9;

            /* cache key */
            uint k0 = k[0];
            uint k1 = k[1];
            uint k2 = k[2];
            uint k3 = k[3];

            /* basic cycle start */
            for (i = 0; i < 32; i++)
            {
                sum += delta;
                v0 += ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
                v1 += ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
            }
            /* end cycle */

            v[0] = v0;
            v[1] = v1;
        }

        /// <summary>
        /// Dencrypt 64 bits with a 128 bit key using TEA.
        /// </summary>
        /// <param name="v">Array of two 32 bit uints to be dencoded in place.</param>
        /// <param name="k">Array of four 32 bit uints to act as key.</param>
        private static void TeaDecrypt(uint[] v, uint[] k)
        {
            /* set up */
            uint v0 = v[0];
            uint v1 = v[1];
            uint sum = 0xC6EF3720;
            uint i;

            /* a key schedule constant */
            uint delta = 0x9e3779b9;

            /* cache key */
            uint k0 = k[0];
            uint k1 = k[1];
            uint k2 = k[2];
            uint k3 = k[3];

            /* basic cycle start */
            for (i = 0; i < 32; i++)
            {
                v1 -= ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
                v0 -= ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
                sum -= delta;
            }
            /* end cycle */

            v[0] = v0;
            v[1] = v1;
        }
    }
}
