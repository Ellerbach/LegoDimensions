// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Argument event for LegoTag.
    /// </summary>
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
            Index = (byte)e.TagIndex;
        }

        /// <summary>
        /// Creates a new instance of the LegoTagEventArgs class.
        /// </summary>
        /// <param name="pad">The pad on which the tag is placed.</param>
        /// <param name="present">True is the tag is present, false if removed.</param>
        /// <param name="uuid">The 7 bytes card UID.</param>
        /// <param name="tag">The Lego Tag, can be either a vehicle or a character.</param>
        /// <param name="index">The index on the portal.</param>
        public LegoTagEventArgs(Pad pad, bool present, byte[] uuid, ILegoTag tag, byte index)
        {
            CardUid = uuid;
            Pad = pad;
            Present = present;
            LegoTag = tag;
            Index = index;
        }

        /// <summary>
        /// Gets or sets the 7 bytes card UID.
        /// </summary>
        public byte[] CardUid { get; set; }

        /// <summary>
        /// Gets or sets the pad on which the tag is placed.
        /// </summary>
        public Pad Pad { get; set; }

        /// <summary>
        /// Gets or sets the index on the portal.
        /// </summary>
        public byte Index { get; set; }

        /// <summary>
        /// Gets or sets the tag is present or not.
        /// </summary>
        public bool Present { get; set; }

        /// <summary>
        /// Gets or sets the Lego Tag, can be either a vehicle or a character.
        /// </summary>
        public ILegoTag? LegoTag { get; set; }
    }
}
