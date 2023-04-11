// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensionsRunner.Actions
{
    public class SetColorAll : IAction
    {
        // SetColorAll(Color padCenter, Color padLeft, Color padRight)
        public new string Name => nameof(SetColorAll);

        public int? Duration { get; set; }

        public string Center { get; set; }

        public string Left { get; set; }

        public string Right { get; set; }

        public override string ToString()
        {
            return $"Name={Name},Center={Center},Left={Left},Right={Right},Duration={Duration}";
        }
    }
}
