using System.Globalization;
using QrCodeGenerator.Helpers;
using QrCodeGenerator.Services;

namespace QrCodeGenerator.Tests;

public class CharacterCountFormatterTests
{
    [Theory]
    [InlineData("ru", 0, "символов")]
    [InlineData("ru", 1, "символ")]
    [InlineData("ru", 2, "символа")]
    [InlineData("ru", 21, "символ")]
    [InlineData("ru", 22, "символа")]
    [InlineData("uk", 22, "символи")]
    [InlineData("en", 1, "character")]
    [InlineData("en", 2, "characters")]
    [InlineData("de", 1, "Zeichen")]
    [InlineData("de", 5, "Zeichen")]
    public void GetPluralForm_UsesCultureRules(string culture, int count, string expectedSuffix)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var result = CharacterCountFormatter.GetPluralForm(count);
            Assert.EndsWith(expectedSuffix, result);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Format_IncludesCount_ForRussian()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru");
            Assert.Equal("42 символа", CharacterCountFormatter.Format(42));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}