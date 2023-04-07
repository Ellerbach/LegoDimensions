// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;

namespace LegoDimensions
{
    public interface ILegoPortal
    {
        /// <summary>
        /// Event when a tag is added or removed.
        /// </summary>
        event EventHandler<LegoTagEventArgs>? LegoTagEvent;

        /// <summary>
        /// Gets or sets the NFC reader enablement.
        /// </summary>
        bool NfcEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to get the tag details once a new tag is added.
        /// </summary>
        /// <remarks>Set this to false if you are only interested in getting the Card UID and no details.</remarks>
        bool GetTagDetails { get; set; }

        /// <summary>
        /// Wakes up the portal.
        /// </summary>
        void WakeUp();

        /// <summary>
        /// Sets a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to set the color.</param>
        /// <param name="color">The color.</param>
        void SetColor(Pad pad, Color color);

        /// <summary>
        /// Gets the color of a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad to get the color.</param>
        /// <returns>The color on the pad.</returns>
        Color GetColor(Pad pad);

        /// <summary>
        /// Sets colors on all the pad at the same time immediatly.
        /// </summary>
        /// <param name="padCenter">The center Pad's color.</param>
        /// <param name="padLeft">The left Pad's color.</param>
        /// <param name="padRight">The right Pad's color.</param>
        void SetColorAll(Color padCenter, Color padLeft, Color padRight);

        /// <summary>
        /// Switches off colors on all the pad at the same time immediatly.
        /// </summary>
        void SwitchOffAll();

        /// <summary>
        /// Flashes a color on a specific Pad.
        /// </summary>
        /// <param name="pad">The Pad(s) to flash.</param>
        /// <param name="flashPad">The flash pad settings.</param>
        void Flash(Pad pad, FlashPad flashPad);
    }
}
