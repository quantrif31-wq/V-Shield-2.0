using API.Services;
using Xunit;

namespace API.Tests;

public sealed class LicensePlateHelperTests
{
    [Theory]
    [InlineData(" 29A-123.45 ", "29A12345")]
    [InlineData("29A 12345", "29A12345")]
    [InlineData("51_LD_1234", "51LD1234")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeForMatch_StripsSeparatorsAndUppercases(string? input, string expected)
    {
        Assert.Equal(expected, LicensePlateHelper.NormalizeForMatch(input));
    }

    [Theory]
    [InlineData(" 29a-123 ", "29A-123")]
    [InlineData(null, "")]
    public void NormalizeForStorage_TrimsAndUppercases(string? input, string expected)
    {
        Assert.Equal(expected, LicensePlateHelper.NormalizeForStorage(input));
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("abc", "abc", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    public void LevenshteinDistance_ComputesExpectedDistance(string a, string b, int expected)
    {
        Assert.Equal(expected, LicensePlateHelper.LevenshteinDistance(a, b));
    }

    [Fact]
    public void FuzzyMatchScore_IsOneForIdenticalAndZeroForBlank()
    {
        Assert.Equal(1.0, LicensePlateHelper.FuzzyMatchScore("29A-123.45", "29A12345"));
        Assert.Equal(0, LicensePlateHelper.FuzzyMatchScore("", "29A12345"));
        Assert.Equal(0, LicensePlateHelper.FuzzyMatchScore(null, "29A12345"));
    }

    [Fact]
    public void FuzzyMatchScore_PartialMatchesAreBetweenZeroAndOne()
    {
        var score = LicensePlateHelper.FuzzyMatchScore("29A12345", "29A12346");
        Assert.InRange(score, 0.5, 1.0);
    }

    [Fact]
    public void GetConfusableVariants_IncludesOriginalAndAlternates()
    {
        var variants = LicensePlateHelper.GetConfusableVariants("0AB");
        Assert.Contains("0AB", variants);
        Assert.Contains("OAB", variants);
        Assert.Contains("QAB", variants);
        Assert.Contains("DAB", variants);
    }

    [Fact]
    public void GetConfusableVariants_EmptyForBlankInput()
    {
        Assert.Empty(LicensePlateHelper.GetConfusableVariants(null));
        Assert.Empty(LicensePlateHelper.GetConfusableVariants("   "));
    }

    [Theory]
    [InlineData("30A-123.45", true)]
    [InlineData("29-A1 1234", true)]
    [InlineData("51LD-12345", true)]
    [InlineData("123", false)]
    [InlineData("ABCDEFGH", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsVietnamesePlateFormat_ValidatesShapes(string? plate, bool expected)
    {
        Assert.Equal(expected, LicensePlateHelper.IsVietnamesePlateFormat(plate));
    }
}
