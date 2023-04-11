// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensionsRunner.Actions
{
    public class FadeAll : IAction
    {
        // void FadeAll(FadePad fadePadCenter, FadePad fadePadLeft, FadePad fadePadRight);
        public new string Name => nameof(FadeAll);

        public int? Duration { get; set; }

        /// <summary>
        /// Gets or sets the value for enabled pad.
        /// </summary>
        public bool CenterEnabled { get; set; } = true;

        /// <summary>
        /// Gets of sets the tick time to fade. The highest, the longer.
        /// </summary>
        public byte CenterTickTime { get; set; }

        /// <summary>
        /// Gets or sets the tick count. Even will stop on old color, odd on the new one.
        /// </summary>
        public byte CenterTickCount { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public string CenterColor { get; set; }
        
        /// <summary>
        /// Gets or sets the value for enabled pad.
        /// </summary>
        public bool LeftEnabled { get; set; } = true;

        /// <summary>
        /// Gets of sets the tick time to fade. The highest, the longer.
        /// </summary>
        public byte LeftTickTime { get; set; }

        /// <summary>
        /// Gets or sets the tick count. Even will stop on old color, odd on the new one.
        /// </summary>
        public byte LeftTickCount { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public string LeftColor { get; set; }  
        
        /// <summary>
        /// Gets or sets the value for enabled pad.
        /// </summary>
        public bool RightEnabled { get; set; } = true;

        /// <summary>
        /// Gets of sets the tick time to fade. The highest, the longer.
        /// </summary>
        public byte RightTickTime { get; set; }

        /// <summary>
        /// Gets or sets the tick count. Even will stop on old color, odd on the new one.
        /// </summary>
        public byte RightTickCount { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public string RightColor { get; set; }

        public override string ToString()
        {
            return $"Name={Name},CenterEnabled={CenterEnabled},CenterTickTime={CenterTickTime},CenterTickCount={CenterTickCount},CenterColor={CenterColor},LeftEnabled={LeftEnabled},LeftTickTime={LeftTickTime},LeftTickCount={LeftTickCount},LeftColor={LeftColor},RightEnabled={RightEnabled},RightTickTime={RightTickTime},RightTickCount={RightTickCount},RightColor={RightColor},Duration={Duration}";
        }
    }
}
