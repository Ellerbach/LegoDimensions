internal static class Xbox360Transport
{
    public static byte[] WrapLegoFrame(ReadOnlySpan<byte> frame)
    {
        if (frame.Length != 32)
        {
            throw new ArgumentException("Xbox 360 LEGO frames must be 32 bytes.", nameof(frame));
        }

        var report = new byte[32];
        report[0] = 0x0B;
        // Confirmed from real toy pad firmware (dopheideb/LEGODimensions); an earlier capture had misread this as 0x14.
        report[1] = 0x16;
        frame[..30].CopyTo(report.AsSpan(2));
        return report;
    }
}