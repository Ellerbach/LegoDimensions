// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LibUsbDotNet;
using System.Runtime.InteropServices;

namespace LegoDimensions
{
    internal static class Imports
    {
        [DllImport("libusb-1.0", EntryPoint = "libusb_set_auto_detach_kernel_driver")]
        public static extern Error SetAutoDetachKernelDriver(DeviceHandle devHandle, int enable);
    }
}
