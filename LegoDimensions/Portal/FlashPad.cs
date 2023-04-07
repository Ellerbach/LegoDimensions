// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Portal
{
    /// <summary>
    /// A class to manage how to flash a pad.
    /// </summary>
    public class FlashPad
    {
        /// <summary>
        /// Creates a Flash Pad class.
        /// </summary>
        public FlashPad()
        { }

        /// <summary>
        /// Creates a Flash Pad class.
        /// </summary>
        /// <param name="tickOn">The time to to stay on. The higher, the longer.</param>
        /// <param name="tickOff">The time to stay off. The higher, the longer.</param>
        /// <param name="tickCount">The number of pulses, 0xFF is forever.</param>
        /// <param name="color">The color.</param>
        /// <param name="enabled">True of pad should be enabled.</param>
        public FlashPad(byte tickOn, byte tickOff, byte tickCount, Color color, bool enabled = true)
        {
            TickOn = tickOn;
            TickOff = tickOff;
            TickCount = tickCount;
            Color = color;
            Enabled = enabled;
        }

        /// <summary>
        /// True if it will flash forever.
        /// </summary>
        public bool FlashForever { get => TickCount == 0xFF; }

        /// <summary>
        /// Sets flashing forever.
        /// </summary>
        public void SetForever() => TickCount = 0xFF;

        /// <summary>
        /// Gets or sets the time to to stay on. The higher, the longer.
        /// </summary>
        public byte TickOn { get; set; }

        /// <summary>
        /// Gets or sets the time to stay off. The higher, the longer.
        /// </summary>
        public byte TickOff { get; set; }

        /// <summary>
        /// Gets or sets the number of pulses, 0xFF is forever.
        /// </summary>
        public byte TickCount { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets of sets if the pad should be enabled.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
