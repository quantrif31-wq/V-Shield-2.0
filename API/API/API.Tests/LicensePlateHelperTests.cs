using API.Services;
using Xunit;

namespace API.Tests;

public sealed class LicensePlateHelperTests
{
    [Theory]
    [InlineData(" 29A-123.45 ", "29A12345")]
    [InlineData("29A 12345", "29A12345")]
    [InlineData("30E-888.88_1", "30E888881")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizeForMatch_StripsSeparators(string? input, string expected)
    {
        Assert.Equal(expected, LicensePlateHelper.NormalizeForMatch(input));
    }

    [Theory]
    [InlineData(" 29a-123.45 ", "29A-123.45")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizeForStorage_TrimsAndUpper(string? input, string expected)
    {
        Assert.Equal(expected, LicensePlateHelper.NormalizeForStorage(input));
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "abc", 0)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("abc", "abd", 1)]
    public void LevenshteinDistance_ReturnsExpected(string a, string b, int expected)
    {
        Assert.Equal(expected, LicensePlateHelper.LevenshteinDistance(a, b));
    }

    [Theory]
    [InlineData("29A12345", "29A12345", 1.0)]
    [InlineData("29A12345", "29A12346", 0.875)]
    [InlineData(null, "29A12345", 0)]
    [InlineData("", "", 0)]
    public void FuzzyMatchScore_ExactIsOneAndMismatchScales(string? a, string? b, double expected)
    {
        var actual = LicensePlateHelper.FuzzyMatchScore(a, b);
        if (expected == 1.0)
            Assert.Equal(1.0, actual);
        else
            Assert.Equal(expected, actual, 3);
    }

    [Fact]
    public void GetConfusableVariants_IncludesOriginalAndSubstitutions()
    {
        var variants = LicensePlateHelper.GetConfusableVariants("0");
        Assert.Contains("0", variants);
        Assert.Contains("O", variants);
        Assert.Contains("Q", variants);
        Assert.Contains("D", variants);
    }

    [Fact]
    public void GetConfusableVariants_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(LicensePlateHelper.GetConfusableVariants(null));
        Assert.Empty(LicensePlateHelper.GetConfusableVariants("  "));
    }

    [Theory]
    [InlineData("29A-123.45", true)]
    [InlineData("30E88888", true)]
    [InlineData("29A1234", true)]
    [InlineData("ABC123", false)]
    [InlineData("29A12345678", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsVietnamesePlateFormat_MatchesPattern(string? plate, bool expected)
    {
        Assert.Equal(expected, LicensePlateHelper.IsVietnamesePlateFormat(plate));
    }
}