// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensionsRunner.Actions
{
    class SwitchOffAll : IAction
    {
        // SwitchOffAll()
        public string Name => nameof(SwitchOffAll);

        public int? Duration { get; set; }

        public override string ToString()
        {
            return $"Name={Name},Duration={Duration}";
        }
    }
}
