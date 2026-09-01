using System.Security.Cryptography;
using Xunit;

namespace XboxPortalProbeTests;

public class Xbox360Xsm3HostTests
{
    private static readonly byte[] Identity = Convert.FromHexString(
        "494B00001704E1115415ED8855210133000080025E048E0203000101C1");

    private static readonly byte[] Challenge = Convert.FromHexString(
        "094000001C0A0F6B0BA118265F833C45134953BD186173CF29DE2CD866E4AE34A99C");

    private static readonly byte[] Response = Convert.FromHexString(
        "494C00002881BD7CB370BD761A2F286ED1F2C38EF90BB28349CB4B24A2906C27B1050AB04709751607E1D7E8AF57");

    [Fact]
    public void AcceptsPublishedVerifyPacket()
    {
        var session = Xbox360Xsm3Host.Create(Identity, Challenge, Response);
        var verify = Convert.FromHexString("09410000105ADD1BA07487B762B7A58F34FFE3D1D9A7");

        session.AcceptVerifyPacket(verify);
        session.ValidateFinalResponse(Convert.FromHexString(
            "494C0000105A9CD672B3708DA7570106502060A9BCDE"));
    }

    [Fact]
    public void GeneratedVerifyPacketPassesVerifier()
    {
        var generator = Xbox360Xsm3Host.Create(Identity, Challenge, Response);
        var verifier = Xbox360Xsm3Host.Create(Identity, Challenge, Response);

        var verify = generator.CreateVerifyPacket(Convert.FromHexString("0102030405060708"));

        verifier.AcceptVerifyPacket(verify);
        Assert.Equal(22, verify.Length);
        Assert.Equal("0941000010", Convert.ToHexString(verify, 0, 5));
        Assert.Equal(0, verify.AsSpan(5).ToArray().Aggregate(0, (checksum, value) => checksum ^ value));
    }

    [Fact]
    public void RejectsResponseFromAnotherSession()
    {
        var changedResponse = (byte[])Response.Clone();
        changedResponse[5] ^= 0x01;
        changedResponse[^1] ^= 0x01;

        Assert.Throws<CryptographicException>(() => Xbox360Xsm3Host.Create(Identity, Challenge, changedResponse));
    }

    [Fact]
    public void ReportsKeyMismatchForLiveLegoPortalResponse()
    {
        var identity = Convert.FromHexString(
            "494B000017FDB758440233852438032000008082C6240050030001016E");
        var challenge = Convert.FromHexString(
            "094000001CDEEB918766B0E3C0B26C056DC867E2E7D6A5DC716F211FB43228A0C289");
        var response = Convert.FromHexString(
            "494C0000289D6C1002B5DCA1E4D45AAB0CF4556EFFB6E1DC71E622CD828763C239DC493BEE297D18D54DADD43164");

        var exception = Assert.Throws<CryptographicException>(
            () => Xbox360Xsm3Host.Create(identity, challenge, response));

        Assert.Contains("different 0x23/0x24 key material", exception.Message);
    }

    [Fact]
    public void ReportsKeyMismatchForDopheidebToypadCapture()
    {
        // From github.com/dopheideb/xbox360-controller-auth LEGO-Dimensions-toypad-dump.md,
        // a separate real console<->toypad MITM capture (different physical unit/session
        // than ReportsKeyMismatchForLiveLegoPortalResponse's own capture above).
        var identity = Convert.FromHexString(
            "494B00001774FF25530E11852538032000008082C624005003000101EA");
        var challenge = Convert.FromHexString(
            "094000001CB69EE4D8F725222CD8D6D252255C79BB264CFDE55BBE5BB3C85A0ED7C9");
        var response = Convert.FromHexString(
            "494C000028B77EAAC65B1E9FCB182573C1EF875F7C4B976F65278BD0C76F94F1B97E6E659272591531B9CA355D5D");

        var exception = Assert.Throws<CryptographicException>(
            () => Xbox360Xsm3Host.Create(identity, challenge, response));

        Assert.Contains("different 0x23/0x24 key material", exception.Message);
    }
}