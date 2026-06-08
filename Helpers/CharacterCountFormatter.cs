using QrCodeGenerator.Services;

namespace QrCodeGenerator.Helpers;

internal static class CharacterCountFormatter
{
    public static string Format(int count) => $"{count} {GetPluralForm(count)}";

    public static string GetPluralForm(int count)
    {
        if (LocalizationService.UsesEnglishPluralRules())
        {
            return count == 1
                ? Properties.Resources.CharacterCountOne
                : Properties.Resources.CharacterCountMany;
        }

        if (LocalizationService.UsesGermanPluralRules() ||
            !LocalizationService.IsSlavicCulture())
        {
            return Properties.Resources.CharacterCountMany;
        }

        var mod10 = count % 10;
        var mod100 = count % 100;

        if (mod100 is >= 11 and <= 14)
        {
            return Properties.Resources.CharacterCountMany;
        }

        return mod10 switch
        {
            1 => Properties.Resources.CharacterCountOne,
            >= 2 and <= 4 => Properties.Resources.CharacterCountFew,
            _ => Properties.Resources.CharacterCountMany
        };
    }
}