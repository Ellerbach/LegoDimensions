// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// The password type for the portal NFC card authentication.
    /// </summary>
    public enum PortalPassword
    {
        /// <summary>
        /// Disabled.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Automatic, this is the default.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Custom password.
        /// </summary>
        Custom = 2,
    }
}
