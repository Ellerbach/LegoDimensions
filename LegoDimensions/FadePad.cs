// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// A fade class to fade pads.
    /// </summary>
    public class FadePad
    {
        /// <summary>
        /// Create a class of fade pad.
        /// </summary>
        public FadePad()
        { }

        /// <summary>
        /// Creates a class of fade pad.
        /// </summary>
        /// <param name="tickTime">>The time to to fade. The higher, the longer.</param>
        /// <param name="tickCount">The tick count. Even will stop on old color, odd on the new one. 0 is never.</param>
        /// <param name="color">The old color to fade from.</param>
        /// <param name="enabled">True if pad should be anabled.</param>
        public FadePad(byte tickTime, byte tickCount, Color color, bool enabled = true)         
        {
            TickTime = tickTime;
            TickCount = tickCount;
            Color = color;
            Enabled = enabled;
        }

        /// <summary>
        /// Gets or sets the value for enabled pad.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets of sets the tick time to fade. The highest, the longer.
        /// </summary>
        public byte TickTime { get; set; }

        /// <summary>
        /// Gets or sets the tick count. Even will stop on old color, odd on the new one.
        /// </summary>
        public byte TickCount { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color { get; set; }
    }
}
