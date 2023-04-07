// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Portal
{
    /// <summary>
    /// The message type.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Normal message. This is the default one.
        /// </summary>
        Normal = 0x55,

        /// <summary>
        /// Event message. Rasied when a Tag is placed or removed.
        /// </summary>
        Event = 0x56,
    }
}
