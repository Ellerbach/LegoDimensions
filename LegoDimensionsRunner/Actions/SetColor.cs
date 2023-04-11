// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions.Portal;

namespace LegoDimensionsRunner.Actions
{
    public class SetColor : IAction
    {
        public int? Duration { get; set; }

        // SetColor(Pad pad, Color color)
        public new string Name => nameof(SetColor);

        public Pad Pad { get; set; }

        public string Color { get; set; }

        public override string ToString()
        {
            return $"Name={Name},Pad={Pad},Color={Color},Duration={Duration}";
        }
    }
}
