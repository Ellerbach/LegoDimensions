// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Portal
{
    /// <summary>
    /// Wraps/unwraps LEGO frames in the 2-byte prefix (0x0B 0x16) used by the Xbox 360 toypad
    /// on top of its 32-byte interrupt reports. No XSM3 security-interface binding is required:
    /// the toypad accepts and replies to LEGO commands over interface 0 without authentication.
    /// </summary>
    internal static class Xbox360Transport
    {
        private const byte FramePrefix0 = 0x0B;
        private const byte FramePrefix1 = 0x16;

        internal static byte[] WrapLegoFrame(ReadOnlySpan<byte> frame)
        {
            if (frame.Length != 32)
            {
                throw new ArgumentException("Xbox 360 LEGO frames must be 32 bytes.", nameof(frame));
            }

            var report = new byte[32];
            report[0] = FramePrefix0;
            report[1] = FramePrefix1;
            frame[..30].CopyTo(report.AsSpan(2));
            return report;
        }

        internal static bool TryUnwrapLegoFrame(ReadOnlySpan<byte> rawFrame, out byte[] standardFrame)
        {
            standardFrame = new byte[32];
            if (rawFrame.Length < 2 || rawFrame[0] != FramePrefix0 || rawFrame[1] != FramePrefix1)
            {
                return false;
            }

            var available = Math.Min(30, rawFrame.Length - 2);
            rawFrame.Slice(2, available).CopyTo(standardFrame);
            return true;
        }
    }
}
