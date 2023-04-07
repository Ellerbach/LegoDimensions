// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace LegoDimensions.Portal
{
    /// <summary>
    /// A message from or to the portal.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the message command.
        /// </summary>
        public MessageCommand MessageCommand { get; set; }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Gets or sets the message source.
        /// </summary>
        public MessageSource MessageSource { get; set; } = MessageSource.Machine;

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        public byte[] Payload { get; set; } = new byte[0];

        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        public byte MessageId { get; set; }

        /// <summary>
        /// Creates a Message.
        /// </summary>
        /// <param name="msgCommand">The message command.</param>
        /// <param name="messageType">The message type.</param>
        /// <param name="payload">The payload.</param>
        public Message(MessageCommand msgCommand, MessageType messageType, byte[] payload) : this(msgCommand, messageType)
        {
            Payload = payload;
        }

        /// <summary>
        /// Creates a Message.
        /// </summary>
        /// <param name="msgCommand">The message command.</param>
        /// <param name="messageType">The message type. By default a normal message.</param>
        public Message(MessageCommand msgCommand, MessageType messageType = MessageType.Normal)
        {
            MessageCommand = msgCommand;
            MessageType = messageType;
        }

        /// <summary>
        /// Creates a message from a message.
        /// </summary>
        /// <param name="readBuffer">The 32 bytes read buffer.</param>
        /// <param name="messageSource">The source of the message.</param>
        /// <returns>A message.</returns>
        /// <exception cref="ArgumentException">Not correct length, invalid type, invalid payload size.</exception>
        public static Message CreateFromBuffer(byte[] readBuffer, MessageSource messageSource)
        {
            // Check if lenght is 32
            if (readBuffer.Length != 32)
            {
                throw new ArgumentException("Message buffer is not 32 bytes.");
            }

            MessageType messageType = (MessageType)readBuffer[0];
            // message type
            if (messageType != MessageType.Normal && messageType != MessageType.Event)
            {
                throw new ArgumentException("Invalid message tyep.");
            }

            int length = readBuffer[1];
            if (length < 1 || length > 31)
            {
                throw new ArgumentException("Invalid payload size");
            }

            if (messageType == MessageType.Normal)
            {
                if (length > 1)
                {
                    if (messageSource == MessageSource.Machine)
                    {
                        // We should have a payload
                        MessageCommand messageCommand = (MessageCommand)readBuffer[2];
                        byte messageId = readBuffer[3];
                        if (readBuffer[length + 2] != GetChecksum(readBuffer.AsSpan(0, length + 2)))
                        {
                            throw new ArgumentException("Invalid checksum");
                        }

                        return new Message(messageCommand, messageType, readBuffer.AsSpan(4, length - 2).ToArray()) { MessageId = messageId, MessageSource = messageSource };
                    }
                    else
                    {
                        // We should have a payload
                        byte messageId = readBuffer[2];
                        if (readBuffer[length + 2] != GetChecksum(readBuffer.AsSpan(0, length + 2)))
                        {
                            throw new ArgumentException("Invalid checksum");
                        }

                        return new Message(MessageCommand.None, messageType, readBuffer.AsSpan(3, length - 1).ToArray()) { MessageId = messageId, MessageSource = messageSource };
                    }
                }
                else
                {
                    if (readBuffer[length + 2] != GetChecksum(readBuffer.AsSpan(0, length + 2)))
                    {
                        throw new ArgumentException("Invalid checksum");
                    }

                    return new Message(MessageCommand.None, messageType, new byte[0]) { MessageSource = messageSource };
                }
            }
            else
            {
                return new Message(MessageCommand.None, messageType, readBuffer.AsSpan(2, length).ToArray()) { MessageSource = messageSource };
            }
        }

        /// <summary>
        /// Add an element to the payload.
        /// </summary>
        /// <param name="args">The elements to add.</param>
        /// <exception cref="ArgumentException"></exception>
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
                    // Needed for boxing/unboxing
                    var pad = (Pad)arg;
                    payload.Add((byte)pad);
                }
                else
                {
                    throw new ArgumentException($"The type {arg.GetType()} is not supported.");
                }
            }

            // 32 - magic message - size - byte - massage ID
            if (payload.Count > 28)
            {
                throw new ArgumentException("Payload is too long.");
            }

            Payload = payload.ToArray();
        }

        /// <summary>
        /// Gets the bytes to send.
        /// </summary>
        /// <param name="messageID">Use the message ID property except if specified.</param>
        /// <returns>The message ID.</returns>
        /// <exception cref="ArgumentException">Payload is too long.</exception>
        public byte[] GetBytes(byte messageID = 0)
        {
            // 32 - magic message - size - byte - massage ID
            if (Payload.Length > 28)
            {
                throw new ArgumentException("Payload is too long.");
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
