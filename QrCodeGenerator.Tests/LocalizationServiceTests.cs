using System.Globalization;
using QrCodeGenerator.Services;

namespace QrCodeGenerator.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void SupportedLanguages_ContainsRequestedCultures()
    {
        var codes = LocalizationService.SupportedLanguages.Select(l => l.Code).ToArray();

        Assert.Contains("en", codes);
        Assert.Contains("de", codes);
        Assert.Contains("ru", codes);
        Assert.Contains("uk", codes);
        Assert.Contains("be", codes);
        Assert.Contains("kk", codes);
        Assert.Contains("uz", codes);
        Assert.Contains("hy", codes);
        Assert.Contains("az", codes);
        Assert.Contains("ka", codes);
        Assert.Contains("ky", codes);
        Assert.Contains("tg", codes);
        Assert.Contains("tk", codes);
        Assert.Contains("ro", codes);
    }

    [Theory]
    [InlineData("ru", true)]
    [InlineData("uk", true)]
    [InlineData("be", true)]
    [InlineData("en", false)]
    [InlineData("de", false)]
    public void IsSlavicCulture_DetectsSlavicLanguages(string culture, bool expected)
    {
        var info = CultureInfo.GetCultureInfo(culture);
        Assert.Equal(expected, LocalizationService.IsSlavicCulture(info));
    }
}