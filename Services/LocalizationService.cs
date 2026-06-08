using System.Globalization;
using System.IO;
using System.Text.Json;

namespace QrCodeGenerator.Services;

public static class LocalizationService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QrCodeGenerator",
        "settings.json");

    public static event Action? CultureChanged;

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("en", "English", "🇬🇧"),
        new("de", "Deutsch", "🇩🇪"),
        new("ru", "Русский", "🇷🇺"),
        new("uk", "Українська", "🇺🇦"),
        new("be", "Беларуская", "🇧🇾"),
        new("kk", "Қазақша", "🇰🇿"),
        new("uz", "Oʻzbekcha", "🇺🇿"),
        new("hy", "Հայերեն", "🇦🇲"),
        new("az", "Azərbaycanca", "🇦🇿"),
        new("ka", "ქართული", "🇬🇪"),
        new("ky", "Кыргызча", "🇰🇬"),
        new("tg", "Тоҷикӣ", "🇹🇯"),
        new("tk", "Türkmençe", "🇹🇲"),
        new("ro", "Română", "🇲🇩")
    ];

    public static CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    public static void Initialize()
    {
        var cultureName = LoadSavedCultureName() ?? GetSystemCultureName();
        ApplyCulture(cultureName, persist: false);
    }

    public static void SetCulture(string cultureName)
    {
        ApplyCulture(cultureName, persist: true);
    }

    public static bool IsSlavicCulture(CultureInfo? culture = null)
    {
        culture ??= CurrentCulture;
        return culture.TwoLetterISOLanguageName is "ru" or "uk" or "be";
    }

    public static bool UsesEnglishPluralRules(CultureInfo? culture = null)
    {
        culture ??= CurrentCulture;
        return culture.TwoLetterISOLanguageName == "en";
    }

    public static bool UsesGermanPluralRules(CultureInfo? culture = null)
    {
        culture ??= CurrentCulture;
        return culture.TwoLetterISOLanguageName == "de";
    }

    private static void ApplyCulture(string cultureName, bool persist)
    {
        var supported = SupportedLanguages.FirstOrDefault(l =>
            l.Code.Equals(cultureName, StringComparison.OrdinalIgnoreCase));

        var culture = supported is null
            ? CultureInfo.GetCultureInfo("en")
            : CultureInfo.GetCultureInfo(supported.Code);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;

        if (persist)
        {
            SaveCultureName(culture.TwoLetterISOLanguageName);
        }

        CultureChanged?.Invoke();
    }

    private static string? LoadSavedCultureName()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings?.Culture;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveCultureName(string cultureName)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = new AppSettings { Culture = cultureName };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
            // Best-effort persistence.
        }
    }

    private static string GetSystemCultureName()
    {
        var system = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return SupportedLanguages.Any(l => l.Code == system) ? system : "en";
    }

    private sealed class AppSettings
    {
        public string? Culture { get; set; }
    }
}

public sealed record LanguageOption(string Code, string DisplayName, string FlagEmoji)
{
    public string LabelWithFlag => $"{FlagEmoji} {DisplayName}";
}