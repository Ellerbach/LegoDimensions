// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensionsRunner.Actions;
using System.Text.Json.Serialization;

namespace LegoDimensionsRunner
{
    public class Animation
    {
        public string Name { get; set; }

        public int? Duration { get; set; }

        public List<dynamic> Actions { get; set; }

        [JsonIgnore]
        public List<Action> CompiledActions { get; set; }

        public int? PortalId { get; set; }
    }
}
