// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensionsRunner.Actions
{
    public class FlashAll : IAction
    {
        // void FlashAll(FlashPad flashPadCenter, FlashPad flashPadLeft, FlashPad flashPadRight);
        public new string Name => nameof(FlashAll);

        public int? Duration { get; set; }

        public byte CenterTickOn { get; set; }

        public byte CenterTickOff { get; set; }

        public byte CenterTickCount { get; set; }

        public string CenterColor { get; set; }

        public bool CenterEnabled { get; set; } = true;

        public byte LeftTickOn { get; set; }

        public byte LeftTickOff { get; set; }

        public byte LeftTickCount { get; set; }

        public string LeftColor { get; set; }

        public bool LeftEnabled { get; set; } = true;

        public byte RightTickOn { get; set; }

        public byte RightTickOff { get; set; }

        public byte RightTickCount { get; set; }

        public string RightColor { get; set; }

        public bool RightEnabled { get; set; } = true;

        public override string ToString()
        {
            return $"Name={Name},CenterTickOn={CenterTickOn},CenterTickOff={CenterTickOff},CenterTickCount={CenterTickCount},CenterColor={CenterColor},CenterEnabled={CenterEnabled}" +
                $",LeftTickOn={LeftTickOn},LeftTickOff={LeftTickOff},LeftTickCount={LeftTickCount},LeftColor={LeftColor},LeftEnabled={LeftEnabled}" +
                $",RightTickOn={RightTickOn},RightTickOff={RightTickOff},RightTickCount={RightTickCount},RightColor={RightColor},RightEnabled={RightEnabled}";
        }
    }
}
