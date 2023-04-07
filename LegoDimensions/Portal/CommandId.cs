// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Portal
{
    internal class CommandId
    {
        public CommandId(byte messageId, MessageCommand messageCommand, ManualResetEvent manualResetEvent)
        {
            MessageId = messageId;
            MessageCommand = messageCommand;
            ManualResetEvent = manualResetEvent;
        }

        public MessageCommand MessageCommand { get; set; }

        public byte MessageId { get; set; }

        public ManualResetEvent ManualResetEvent { get; set; }

        public object? Result { get; set; }
    }
}
