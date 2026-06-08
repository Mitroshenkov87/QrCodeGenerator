using System.IO;
using QRCoder.Exceptions;
using QrCodeGenerator.Services;

namespace QrCodeGenerator.Tests;

public class QrCodeServiceTests
{
    private readonly QrCodeService _service = new();

    [Fact]
    public void GenerateQrCode_ReturnsValidBitmapSource()
    {
        StaThread.Run(() =>
        {
            var result = _service.GenerateQrCode("https://example.com");

            Assert.NotNull(result);
            Assert.True(result.PixelWidth > 0);
            Assert.True(result.PixelHeight > 0);
            Assert.True(result.IsFrozen);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCode_ThrowsForInvalidInput(string? input)
    {
        StaThread.Run(() =>
        {
            Assert.ThrowsAny<ArgumentException>(() => _service.GenerateQrCode(input!));
        });
    }

    [Fact]
    public void GenerateQrCode_HandlesUnicodeText()
    {
        StaThread.Run(() =>
        {
            var result = _service.GenerateQrCode("Привет, мир! 🌍");
            Assert.NotNull(result);
        });
    }

    [Fact]
    public void SaveAsPng_WritesValidPngFile()
    {
        StaThread.Run(() =>
        {
            var bitmap = _service.GenerateQrCode("test");
            var path = Path.Combine(Path.GetTempPath(), $"qrcode-{Guid.NewGuid():N}.png");

            try
            {
                _service.SaveAsPng(bitmap, path);

                Assert.True(File.Exists(path));
                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 8);
                Assert.Equal(0x89, bytes[0]);
                Assert.Equal(0x50, bytes[1]);
                Assert.Equal(0x4E, bytes[2]);
                Assert.Equal(0x47, bytes[3]);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        });
    }

    [Fact]
    public void SaveAsPng_DoesNotLeaveCorruptFileOnFailure()
    {
        StaThread.Run(() =>
        {
            var bitmap = _service.GenerateQrCode("test");
            var invalidPath = Path.Combine("Z:\\nonexistent\\drive", "qrcode.png");

            Exception? exception = null;
            try
            {
                _service.SaveAsPng(bitmap, invalidPath);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.False(File.Exists(invalidPath));
        });
    }

    [Fact]
    public void GenerateQrCode_ThrowsForExcessiveLength()
    {
        StaThread.Run(() =>
        {
            var hugeText = new string('A', 10000);
            Assert.Throws<DataTooLongException>(() => _service.GenerateQrCode(hugeText));
        });
    }
}

internal static class StaThread
{
    public static void Run(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }
}