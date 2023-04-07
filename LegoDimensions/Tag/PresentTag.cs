// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Tag;

namespace LegoDimensions.Portal
{
    /// <summary>
    /// Information regarding a present tag.
    /// </summary>
    public class PresentTag
    {
        /// <summary>
        /// Creates a class with key information regarding a present tag.
        /// </summary>
        /// <param name="pad">The pad the tag is on.</param>
        /// <param name="tagType">The type or tag, normal or uninitialized.</param>
        /// <param name="index">The index of the tag on the portal.</param>
        public PresentTag(Pad pad, TagType tagType, byte index)
        {
            Pad = pad;
            TagType = tagType;
            Index = index;
        }

        /// <summary>
        /// Gets or sets the pad the tag is on.
        /// </summary>
        public Pad Pad { get; set; }

        /// <summary>
        /// Gets or sets the type of tag.
        /// </summary>
        public TagType TagType { get; set; }

        /// <summary>
        /// Gets or sets the index of the tag on the portal.
        /// </summary>
        public byte Index { get; set; }
    }
}

