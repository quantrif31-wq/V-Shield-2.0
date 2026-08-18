using API.Services;
using Xunit;

namespace API.Tests;

public sealed class StaticVisitorQrServiceTests
{
    private readonly StaticVisitorQrService _service = new();

    [Fact]
    public void GenerateSecret_ReturnsBase32Token()
    {
        var secret = _service.GenerateSecret();
        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.All(secret, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(20)]
    public void GenerateSecret_RespectsLength(int length)
    {
        var secret = _service.GenerateSecret(length);
        Assert.Equal((int)Math.Ceiling(length * 8.0 / 5), secret.Length);
    }

    [Fact]
    public void GetCurrentCounter_IncrementsPerTimeStep()
    {
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var d1 = _service.GetCurrentCounter(t0);
        var d2 = _service.GetCurrentCounter(t0.AddSeconds(1));
        var d3 = _service.GetCurrentCounter(t0.AddSeconds(31));
        Assert.Equal(d1, d2);
        Assert.Equal(d2 + 1, d3);
    }

    [Fact]
    public void GenerateOtp_IsDeterministic_And_DigitsPadded()
    {
        var secret = _service.GenerateSecret();
        var counter = _service.GetCurrentCounter(DateTime.UtcNow);
        var first = _service.GenerateOtp(secret, counter);
        var second = _service.GenerateOtp(secret, counter);
        Assert.Equal(first, second);
        Assert.Equal(6, first.Length);
        Assert.True(first.All(char.IsDigit));
    }

    [Fact]
    public void GenerateOtp_DifferentCounter_DifferentOtp()
    {
        var secret = _service.GenerateSecret();
        Assert.NotEqual(
            _service.GenerateOtp(secret, 12345),
            _service.GenerateOtp(secret, 12346));
    }

    [Fact]
    public void BuildPayload_And_ParsePayload_RoundTrip()
    {
        const int visitorId = 7;
        const int regId = 3;
        const long counter = 1234567;
        var secret = _service.GenerateSecret();
        var otp = _service.GenerateOtp(secret, counter);

        var payload = _service.BuildPayload(visitorId, regId, counter, otp);
        Assert.True(_service.TryParsePayload(payload, out var parsed, out var message));

        Assert.NotNull(parsed);
        Assert.Equal(visitorId, parsed.VisitorId);
        Assert.Equal(regId, parsed.RegistrationId);
        Assert.Equal(counter, parsed.Counter);
        Assert.Equal(otp, parsed.Otp);
        Assert.Equal("OK", message);
    }

    [Fact]
    public void TryParsePayload_RejectsEmpty()
    {
        Assert.False(_service.TryParsePayload(null, out var parsed, out var message));
        Assert.Null(parsed);
        Assert.Contains("không được để trống", message);
    }

    [Theory]
    [InlineData("VIS:1|REG:2|TS:3")]
    [InlineData("XX:1|REG:2|TS:3|OTP:4")]
    [InlineData("VIS:abc|REG:2|TS:3|OTP:4")]
    [InlineData("VIS:1|REG:2|TS:xyz|OTP:4")]
    [InlineData("VIS:1|REG:2|TS:3|OTP:")]
    public void TryParsePayload_RejectsMalformed(string payload)
    {
        Assert.False(_service.TryParsePayload(payload, out var parsed, out _));
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParsePayload_IsCaseInsensitiveForIntro()
    {
        Assert.True(_service.TryParsePayload("vis:1|reg:2|ts:3|otp:123456", out var parsed, out _));
        Assert.NotNull(parsed);
        Assert.Equal(1, parsed.VisitorId);
    }

    [Fact]
    public void FixedTimeEquals_ComparesSafely()
    {
        Assert.True(_service.FixedTimeEquals("abc", "abc"));
        Assert.False(_service.FixedTimeEquals("abc", "abd"));
        Assert.False(_service.FixedTimeEquals("abc", null));
        Assert.False(_service.FixedTimeEquals(null, "abc"));
        Assert.False(_service.FixedTimeEquals("short", "muchlonger"));
    }
}