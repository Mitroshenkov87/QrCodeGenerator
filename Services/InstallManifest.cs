using System.Globalization;
using System.IO;
using System.Text;

namespace QrCodeGenerator.Services;

public sealed class InstallManifestData
{
    public required InstallLocationType Scope { get; init; }
    public required string InstallPath { get; init; }
    public required DateTime InstalledAt { get; init; }
    public required string Version { get; init; }
    public string? ShortcutPath { get; init; }
    public string? UserName { get; init; }
}

public static class InstallManifest
{
    public const string FileName = "install.ini";
    private const string SectionName = "Install";

    public static bool ExistsIn(string installDirectory) =>
        File.Exists(GetManifestPath(installDirectory));

    public static string GetManifestPath(string installDirectory) =>
        Path.Combine(installDirectory, FileName);

    public static void Write(string installDirectory, InstallManifestData data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{SectionName}]");
        builder.AppendLine($"Scope={data.Scope}");
        builder.AppendLine($"InstallPath={data.InstallPath}");
        builder.AppendLine($"InstalledAt={data.InstalledAt:O}");
        builder.AppendLine($"Version={data.Version}");
        builder.AppendLine($"ShortcutPath={data.ShortcutPath}");
        builder.AppendLine($"UserName={data.UserName}");
        File.WriteAllText(GetManifestPath(installDirectory), builder.ToString(), Encoding.UTF8);
    }

    public static InstallManifestData? Read(string installDirectory)
    {
        var manifestPath = GetManifestPath(installDirectory);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var values = ParseIni(File.ReadAllLines(manifestPath));
        if (!values.TryGetValue("Scope", out var scopeValue) ||
            !Enum.TryParse<InstallLocationType>(scopeValue, ignoreCase: true, out var scope))
        {
            return null;
        }

        if (!values.TryGetValue("InstallPath", out var installPath) ||
            string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        _ = DateTime.TryParse(
            values.GetValueOrDefault("InstalledAt"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var installedAt);

        return new InstallManifestData
        {
            Scope = scope,
            InstallPath = installPath,
            InstalledAt = installedAt == default ? File.GetLastWriteTimeUtc(manifestPath) : installedAt,
            Version = values.GetValueOrDefault("Version") ?? "unknown",
            ShortcutPath = values.GetValueOrDefault("ShortcutPath"),
            UserName = values.GetValueOrDefault("UserName")
        };
    }

    private static Dictionary<string, string> ParseIni(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                inSection = string.Equals(line[1..^1], SectionName, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }
}