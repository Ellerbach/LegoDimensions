// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Color pulse for flashing.
    /// </summary>
    public enum ColorPulse
    {
        /// <summary>
        /// Forever.
        /// </summary>
        Forever = 0,

        /// <summary>
        /// Stop at the new color.
        /// </summary>
        StopsStaysAtNew = 0x15,

        /// <summary>
        /// Stop at the original color.
        /// </summary>
        StopsStaysAtOriginal = 0x16,

        /// <summary>
        /// Stop at the new color and return to the original color.
        /// </summary>
        StopsReturningToOriginal = 0xC6,

        /// <summary>
        /// Stop at the original color and return to the new color.
        /// </summary>
        StopsReturningToNew = 0xC7,
    }
}
