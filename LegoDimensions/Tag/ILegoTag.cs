// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Tag
{
    public interface ILegoTag
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the world.
        /// </summary>
        public string World { get; set; }

        /// <summary>
        /// Gets or sets the list of abilities.
        /// </summary>
        public List<string> Abilities { get; set; }
    }
}
