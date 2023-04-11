// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;

namespace LegoDimensionsRunner.Actions
{
    public class Fade : IAction
    {
        // void Fade(Pad pad, FadePad fadePad);
        public new string Name => nameof(Fade);

        public int? Duration { get; set; }

        public Pad Pad { get; set; }

        public bool Enabled { get; set; } = true;

        public byte TickTime { get; set; }

        public byte TickCount { get; set; }

        public string Color { get; set; }

        public override string ToString()
        {
            return $"Name={Name},Pad={Pad},Color={Color},Duration={Duration},Enabled={Enabled},TickTime={TickTime},TickCount={TickCount}";
        }
    }
}
