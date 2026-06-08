using System.IO;
using System.Reflection;

namespace QrCodeGenerator.Services;

public enum InstallLocationType
{
    Local,
    AllUsers,
    Roaming // Legacy installs only; no longer offered in the UI.
}

public sealed class AppInstallService
{
    private const string AppFolderName = "QrCodeGenerator";
    private const string StartMenuFolderName = "QR Code Generator";
    private const string ExecutableName = "QrCodeGenerator.exe";

    public IReadOnlyList<InstallLocationType> SelectableLocationTypes { get; } =
        [InstallLocationType.Local, InstallLocationType.AllUsers];

    private static readonly InstallLocationType[] KnownInstallLocations =
        [InstallLocationType.Local, InstallLocationType.AllUsers, InstallLocationType.Roaming];

    public string GetInstallDirectory(InstallLocationType location) => location switch
    {
        InstallLocationType.Local => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            AppFolderName),
        InstallLocationType.Roaming => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName),
        InstallLocationType.AllUsers => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            AppFolderName),
        _ => throw new ArgumentOutOfRangeException(nameof(location))
    };

    public string GetStartMenuShortcutPath(InstallLocationType location) => location switch
    {
        InstallLocationType.AllUsers => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            $"{StartMenuFolderName}.lnk"),
        _ => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            $"{StartMenuFolderName}.lnk")
    };

    public bool IsInstalled()
    {
        if (TryGetInstalledManifest(out _))
        {
            return true;
        }

        foreach (var location in KnownInstallLocations)
        {
            var directory = GetInstallDirectory(location);
            if (InstallManifest.ExistsIn(directory))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetInstalledManifest(out InstallManifestData? manifest)
    {
        var currentDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (InstallManifest.ExistsIn(currentDirectory))
        {
            manifest = InstallManifest.Read(currentDirectory);
            return manifest is not null;
        }

        foreach (var location in KnownInstallLocations)
        {
            var directory = GetInstallDirectory(location);
            if (!InstallManifest.ExistsIn(directory))
            {
                continue;
            }

            manifest = InstallManifest.Read(directory);
            if (manifest is not null)
            {
                return true;
            }
        }

        manifest = null;
        return false;
    }

    public InstallResult Install(InstallLocationType location)
    {
        var sourceDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetDirectory = GetInstallDirectory(location);
        var executablePath = Path.Combine(targetDirectory, ExecutableName);
        var shortcutPath = GetStartMenuShortcutPath(location);

        try
        {
            Directory.CreateDirectory(targetDirectory);
            CopyDirectory(sourceDirectory, targetDirectory);

            var manifest = new InstallManifestData
            {
                Scope = location,
                InstallPath = targetDirectory,
                InstalledAt = DateTime.UtcNow,
                Version = GetAppVersion(),
                ShortcutPath = shortcutPath,
                UserName = Environment.UserName
            };

            InstallManifest.Write(targetDirectory, manifest);

            try
            {
                CreateShortcut(
                    shortcutPath,
                    executablePath,
                    targetDirectory,
                    Properties.Resources.AppTitle,
                    executablePath);
            }
            catch (Exception shortcutEx) when (location == InstallLocationType.AllUsers)
            {
                return new InstallResult(
                    true,
                    targetDirectory,
                    shortcutPath,
                    string.Format(Properties.Resources.InstallShortcutWarning, shortcutEx.Message));
            }

            return new InstallResult(true, targetDirectory, shortcutPath, null);
        }
        catch (Exception ex)
        {
            return new InstallResult(false, targetDirectory, shortcutPath, ex.Message);
        }
    }

    private static string GetAppVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        var normalizedSource = Path.GetFullPath(sourceDirectory);
        var normalizedTarget = Path.GetFullPath(targetDirectory);

        if (normalizedSource.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            if (string.Equals(fileName, InstallManifest.FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string workingDirectory,
        string description,
        string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new PlatformNotSupportedException("WScript.Shell is not available.");

        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Failed to create WScript.Shell.");

        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = description;
        shortcut.IconLocation = iconPath;
        shortcut.Save();

        if (shortcut is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (shell is IDisposable shellDisposable)
        {
            shellDisposable.Dispose();
        }
    }
}

public sealed record InstallResult(
    bool Success,
    string InstallDirectory,
    string ShortcutPath,
    string? ErrorMessage);