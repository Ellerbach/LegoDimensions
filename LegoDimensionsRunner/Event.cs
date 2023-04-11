using LegoDimensions.Portal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsRunner
{
    public class Event
    {
        public string? CardId { get; set; }

        public ushort? Id { get; set; }

        public Pad Pad { get; set; }

        public string Animation { get; set; }

        public int? PortalId { get; set; }
    }
}
