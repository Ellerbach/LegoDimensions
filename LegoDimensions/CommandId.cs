using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensions
{
    internal class CommandId
    {
        public CommandId(byte messageId, MessageCommand messageCommand)
        {
            MessageId = messageId;
            MessageCommand = messageCommand;
        }

        public MessageCommand MessageCommand { get; set; }

        public byte MessageId { get; set; }
    }
}
