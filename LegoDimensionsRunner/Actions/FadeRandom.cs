// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;

namespace LegoDimensionsRunner.Actions
{
    public class FadeRandom : IAction
    {
        // void FadeRandom(Pad pad, byte tickTime, byte tickCount);
        public new string Name => nameof(FadeRandom);

        public int? Duration { get; set; }

        public Pad Pad { get; set; }

        public byte TickTime { get; set; }

        public byte TickCount { get; set; }

        public override string ToString()
        {
            return $"Name={Name},Pad={Pad},Duration={Duration},TickTime={TickTime},TickCount={TickCount}";
        }
    }

}
