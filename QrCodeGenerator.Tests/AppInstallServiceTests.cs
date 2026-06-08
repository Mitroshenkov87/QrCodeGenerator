using System.IO;
using QrCodeGenerator.Services;

namespace QrCodeGenerator.Tests;

public class AppInstallServiceTests
{
    private readonly AppInstallService _service = new();

    [Fact]
    public void GetInstallDirectory_Local_UsesAppDataLocalPrograms()
    {
        var path = _service.GetInstallDirectory(InstallLocationType.Local);
        Assert.Contains("Programs", path);
        Assert.Contains("QrCodeGenerator", path);
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            path);
    }

    [Fact]
    public void SelectableLocationTypes_OnlyIncludesLocalAndProgramFiles()
    {
        Assert.Equal(
            [InstallLocationType.Local, InstallLocationType.AllUsers],
            _service.SelectableLocationTypes);
    }

    [Fact]
    public void GetInstallDirectory_AllUsers_UsesProgramFiles()
    {
        var path = _service.GetInstallDirectory(InstallLocationType.AllUsers);
        Assert.EndsWith("QrCodeGenerator", path);
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            path);
    }

    [Fact]
    public void GetStartMenuShortcutPath_AllUsers_UsesCommonPrograms()
    {
        var path = _service.GetStartMenuShortcutPath(InstallLocationType.AllUsers);
        Assert.Contains("Programs", path);
        Assert.EndsWith("QR Code Generator.lnk", path);
    }

    [Fact]
    public void IsInstalled_ReturnsTrueWhenManifestExistsInCurrentDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"qr-install-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            InstallManifest.Write(tempDir, new InstallManifestData
            {
                Scope = InstallLocationType.Local,
                InstallPath = tempDir,
                InstalledAt = DateTime.UtcNow,
                Version = "1.0.0"
            });

            Assert.True(InstallManifest.ExistsIn(tempDir));
            var manifest = InstallManifest.Read(tempDir);
            Assert.NotNull(manifest);
            Assert.Equal(InstallLocationType.Local, manifest!.Scope);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}