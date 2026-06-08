using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using QRCoder.Exceptions;
using QRGen = QRCoder.QRCodeGenerator;

namespace QrCodeGenerator.Services;

/// <summary>
/// Encapsulates QR code generation using QRCoder and converts results for WPF display.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    private const int BasePixelsPerModule = 12;
    private const int MaxBitmapDimension = 1200;

    public BitmapSource GenerateQrCode(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var byteCount = Encoding.UTF8.GetByteCount(text);
        var levels = EccLevelSelector.GetLevelsToTry(byteCount);

        using var generator = new QRGen();
        using var data = CreateQrCodeData(generator, text, levels);
        using var qrCode = new QRCode(data);

        var moduleCount = data.ModuleMatrix.Count;
        var pixelsPerModule = CalculatePixelsPerModule(moduleCount);
        using var bitmap = qrCode.GetGraphic(pixelsPerModule);

        return ConvertToBitmapSource(bitmap);
    }

    public void SaveAsPng(BitmapSource source, string filePath)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
            filePath = Path.Combine(directory, Path.GetFileName(filePath));
        }

        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = File.Create(tempPath))
            {
                encoder.Save(stream);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static QRCodeData CreateQrCodeData(
        QRGen generator,
        string text,
        IReadOnlyList<QRGen.ECCLevel> levels)
    {
        DataTooLongException? lastException = null;

        foreach (var level in levels)
        {
            try
            {
                return generator.CreateQrCode(text, level);
            }
            catch (DataTooLongException ex)
            {
                lastException = ex;
            }
        }

        throw lastException ?? new DataTooLongException(text, "Byte", Encoding.UTF8.GetByteCount(text));
    }

    private static int CalculatePixelsPerModule(int moduleCount)
    {
        var dimension = moduleCount * BasePixelsPerModule;
        if (dimension <= MaxBitmapDimension)
        {
            return BasePixelsPerModule;
        }

        return Math.Max(2, MaxBitmapDimension / moduleCount);
    }

    private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }
}