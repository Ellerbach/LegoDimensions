// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    public class LegoTagEventArgs : EventArgs
    {
        
        internal LegoTagEventArgs(PadTag e)
        {
            // To make a copy of the event args
            CardUid = new byte[e.CardUid.Length];
            e.CardUid.CopyTo(CardUid, 0);
            Pad = e.Pad;
            Present = e.Present;
            LegoTag = e.LegoTag;
        }

        public LegoTagEventArgs(Pad pad, bool present, byte[] uuid, ILegoTag tag)
        {
            CardUid = uuid;
            Pad = pad;
            Present = present;
            LegoTag = tag;
        }

        public byte[] CardUid { get; set; }

        public Pad Pad { get; set; }

        public bool Present { get; set; }

        public ILegoTag? LegoTag { get; set; }
    }
}
