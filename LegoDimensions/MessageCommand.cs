// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    public enum MessageCommand
    {
        None = 0x00,
        // B = general
        Wake = 0xB0,
        Seed = 0xB1,
        Challenge = 0xB3,
        // C = colors
        Color = 0xC0,
        GetCol = 0xC1,
        Fade = 0xC2,
        Flash = 0xC3,
        FadeRandom = 0xC4,
        FadeUnknown = 0xC5,
        FadeAll = 0xC6,
        FlashAll = 0xC7,
        ColorAll = 0xC8,
        // D = tags
        TagList = 0xD0,
        Read = 0xD2,
        Write = 0xD3,
        Model = 0xD4,
        // E = configuration
        ConfigPassword = 0xE1,
        ConfigActive = 0xE5,
        LEDSQ = 0xFF,
    }
}
