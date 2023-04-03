// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Message commands that can be sent to the portal.
    /// </summary>
    public enum MessageCommand
    {
        /// <summary>No command, this is used when receiveing a message from the portal.</summary>
        None = 0x00,

        // B = general
        /// <summary>Wake up command.</summary>
        Wake = 0xB0,

        /// <summary>Generate a seed.</summary>
        Seed = 0xB1,

        /// <summary>Challenge.</summary>
        Challenge = 0xB3,

        // C = colors
        /// <summary>Change color immediatly.</summary>
        Color = 0xC0,

        /// <summary>Get a color.</summary>
        GetColor = 0xC1,

        /// <summary>Fade colors.</summary>
        Fade = 0xC2,

        /// <summary>Flash colors.</summary>
        Flash = 0xC3,

        /// <summary>Fade to a random color.</summary>
        FadeRandom = 0xC4,

        /// <summary>Fade unkwon?</summary>
        FadeUnknown = 0xC5,

        /// <summary>Fade all.</summary>
        FadeAll = 0xC6,

        /// <summary>Flash all.</summary>
        FlashAll = 0xC7,

        /// <summary>Color all pads.</summary>
        ColorAll = 0xC8,

        // D = tags
        /// <summary>List tags</summary>
        TagList = 0xD0,

        /// <summary>Read 16 bytes on a specific page.</summary>
        Read = 0xD2,

        /// <summary>Write 16 bytes on a specific page.</summary>
        Write = 0xD3,

        /// <summary></summary>
        Model = 0xD4,

        // E = configuration
        /// <summary></summary>
        ConfigPassword = 0xE1,

        /// <summary></summary>
        ConfigActive = 0xE5,

        /// <summary></summary>
        LEDSQ = 0xFF,
    }
}
