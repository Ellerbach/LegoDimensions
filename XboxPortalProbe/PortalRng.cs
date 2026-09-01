/// <summary>The ARX pseudo-random generator the portal firmware uses while handling 0xB1/0xB3/0xB4.</summary>
internal sealed class PortalRng
{
    // "f1ea 5eed" is leetspeak for "flea seed"; found verbatim in the toy pad firmware.
    private const uint FleaSeed = 0xF1EA5EED;
    private const int WarmupRounds = 42;

    private uint _s0;
    private uint _s1;
    private uint _s2;
    private uint _s3;

    public PortalRng(uint seed)
    {
        _s0 = FleaSeed;
        _s1 = _s2 = _s3 = seed;
        for (var i = 0; i < WarmupRounds; i++)
        {
            Next();
        }
    }

    public uint Next()
    {
        var temp = unchecked(_s0 - RotateLeft(_s1, 21));
        _s0 = _s1 ^ RotateLeft(_s2, 19);
        _s1 = unchecked(_s2 + RotateLeft(_s3, 6));
        _s2 = unchecked(_s3 + temp);
        _s3 = unchecked(_s0 + temp);
        return _s3;
    }

    private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));
}
