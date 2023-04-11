// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;

namespace LegoDimensionsRunner.Actions
{
    public class Flash : IAction
    {
        // Flash(Pad pad, FlashPad flashPad)
        public new string Name => nameof(Flash);

        public int? Duration { get; set; }

        public Pad Pad { get; set; }

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
        /// Gets or sets the string representation of the color.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets of sets if the pad should be enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        public override string ToString()
        {
            return $"Name={Name},Pad={Pad},Color={Color},Duration={Duration},Enabled={Enabled},TickOn={TickOn},TickOff={TickOff},TickCount={TickCount}";
        }
    }
}
