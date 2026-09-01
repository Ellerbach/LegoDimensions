using Xunit;

namespace XboxPortalProbeTests;

public class PortalRngTests
{
    [Fact]
    public void SameSeedProducesSameSequence()
    {
        var first = new PortalRng(0x12345678);
        var second = new PortalRng(0x12345678);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(first.Next(), second.Next());
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentSequences()
    {
        var first = new PortalRng(0x00000001);
        var second = new PortalRng(0x00000002);

        Assert.NotEqual(first.Next(), second.Next());
    }

    [Theory]
    [InlineData(0x00000000U)]
    [InlineData(0xDEADBEEFU)]
    [InlineData(0x12345678U)]
    public void MatchesIndependentReferenceImplementation(uint seed)
    {
        var rng = new PortalRng(seed);
        var reference = new ReferenceRng(seed);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(reference.Next(), rng.Next());
        }
    }

    // Independent transcription of the ARX algorithm (tuple-based state instead of
    // fields), used only to cross-check PortalRng without sharing its implementation.
    private sealed class ReferenceRng
    {
        private (uint A, uint B, uint C, uint D) _state;

        public ReferenceRng(uint seed)
        {
            _state = (0xF1EA5EEDU, seed, seed, seed);
            for (var i = 0; i < 42; i++)
            {
                Next();
            }
        }

        public uint Next()
        {
            static uint Rotl(uint value, int count) => (value << count) | (value >> (32 - count));

            unchecked
            {
                var e = _state.A - Rotl(_state.B, 21);
                var a = _state.B ^ Rotl(_state.C, 19);
                var b = _state.C + Rotl(_state.D, 6);
                var c = _state.D + e;
                var d = a + e;
                _state = (a, b, c, d);
                return d;
            }
        }
    }
}
