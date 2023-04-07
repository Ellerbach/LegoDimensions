// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Tag;

namespace LegoDimensions.Portal
{
    internal class PadTag
    {
        public int TagIndex { get; set; }

        public byte[] CardUid { get; set; }

        public Pad Pad { get; set; }

        public bool Present { get; set; }

        public byte LastMessageId { get; set; }

        public ILegoTag? LegoTag { get; set; }

        public TagType TagType { get; set; }
    }
}
