using System.Windows.Media.Imaging;

namespace QrCodeGenerator.Services;

public interface IQrCodeService
{
    BitmapSource GenerateQrCode(string text);

    void SaveAsPng(BitmapSource source, string filePath);
}