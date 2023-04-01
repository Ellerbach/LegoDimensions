// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace LegoDimensions
{
    public class Message
    {
        public MessageCommand MessageCommand { get; set; }

        public MessageType MessageType { get; set; }

        public byte[] Payload { get; set; } = new byte[0];

        public byte MessageId { get; set; }

        public Message(MessageCommand portalCommand, MessageType messageType, byte[] payload) : this(portalCommand, messageType)
        {
            Payload = payload;
        }

        public Message(MessageCommand command, MessageType messageType = MessageType.Normal)
        {
            MessageCommand = command;
            MessageType = messageType;
        }

        public static Message CreateFromBuffer(byte[] readBuffer)
        {
            // Check if lenght is 32
            if (readBuffer.Length != 32)
            {
                throw new ArgumentException("Message buffer is not 32 bytes.");
            }

            MessageType messageType = (MessageType)readBuffer[0];
            // message type
            if ((messageType != MessageType.Normal) && (messageType != MessageType.Event))
            {
                throw new ArgumentException("Invalid message tyep.");
            }

            int length = readBuffer[1];
            if ((length < 1) || (length > 31))
            {
                throw new ArgumentException("Invalid payload size");
            }

            if (messageType == MessageType.Normal)
            {
                MessageCommand messageCommand = (MessageCommand)readBuffer[2];
                byte messageId = readBuffer[3];
                if (readBuffer[length + 2] != GetChecksum(readBuffer.AsSpan(0, length + 2)))
                {
                    throw new ArgumentException("Invalid checksum");
                }

                return new Message(messageCommand, messageType, readBuffer.AsSpan(4, length - 2).ToArray()) { MessageId = messageId };
            }
            else
            {
                return new Message(MessageCommand.None, messageType, readBuffer.AsSpan(2, length).ToArray());
            }
        }

        public void AddPayload(params object[] args)
        {
            List<byte> payload = new List<byte>();
            payload.AddRange(Payload);

            foreach (var arg in args)
            {
                if (arg.GetType() == typeof(byte[]))
                {
                    payload.AddRange((byte[])arg);
                }
                else if (arg.GetType() == typeof(bool))
                {
                    payload.Add((byte)((bool)arg ? 1 : 0));
                }
                else if (arg.GetType() == typeof(string))
                {
                    var bytes = Encoding.ASCII.GetBytes((string)arg);
                    payload.AddRange(bytes);
                }
                else if (arg.GetType() == typeof(byte))
                {
                    payload.Add((byte)arg);
                }
                else if (arg.GetType() == typeof(ushort))
                {
                    Span<byte> bytes = stackalloc byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)arg);
                    payload.AddRange(bytes.ToArray());
                }
                else if (arg.GetType() == typeof(uint))
                {
                    Span<byte> bytes = stackalloc byte[4];
                    BinaryPrimitives.WriteUInt32BigEndian(bytes, (ushort)arg);
                    payload.AddRange(bytes.ToArray());
                }
                else if (arg.GetType() == typeof(Color))
                {
                    Color col = (Color)arg;
                    payload.Add(col.R);
                    payload.Add(col.G);
                    payload.Add(col.B);
                }
                else if (arg.GetType() == typeof(Pad))
                {
                    payload.Add((byte)arg);
                }
                else
                {
                    throw new ArgumentException($"The type {arg.GetType()} is not supported.");
                }
            }

            // 32 - magic message - size - byte - massage ID
            if (payload.Count > 28)
            {
                throw new ArgumentException("Payload is too long");
            }

            Payload = payload.ToArray();
        }

        public byte[] GetBytes(byte messageID = 0)
        {
            // 32 - magic message - size - byte - massage ID
            if (Payload.Length > 28)
            {
                throw new ArgumentException("Payload is too long");
            }

            // The message is 32 bytes
            byte[] bytes = new byte[32];
            int inc = 0;
            bytes[inc++] = (byte)MessageType;
            bytes[inc++] = (byte)(2 + Payload.Length);
            bytes[inc++] = (byte)MessageCommand;
            bytes[inc++] = messageID == 0 ? MessageId : messageID;
            Array.Copy(Payload, 0, bytes, inc, Payload.Length);
            inc += Payload.Length;
            bytes[inc] = GetChecksum(bytes.AsSpan(0, inc));

            return bytes;
        }

        private static byte GetChecksum(Span<byte> bytes)
        {
            byte sum = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }

            return sum;
        }
    }
}
