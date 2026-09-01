using LegoDimensions.Portal;
using Xunit;

namespace XboxPortalProbeTests;

public class Xbox360TransportTests
{
    [Fact]
    public void WrapsWakeFrame()
    {
        var wake = new Message(MessageCommand.Wake);
        wake.AddPayload("(c) LEGO 2014");

        var report = Xbox360Transport.WrapLegoFrame(wake.GetBytes());

        Assert.Equal(
            "0B16550FB000286329204C45474F2032303134F6000000000000000000000000",
            Convert.ToHexString(report));
    }

    [Fact]
    public void RejectsNonStandardFrameLength()
    {
        Assert.Throws<ArgumentException>(() => Xbox360Transport.WrapLegoFrame(new byte[30]));
    }
}