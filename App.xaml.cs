using System.Windows;
using QrCodeGenerator.Services;

namespace QrCodeGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LocalizationService.Initialize();
        base.OnStartup(e);

        IQrCodeService qrCodeService = new QrCodeService();
        var mainWindow = new MainWindow(qrCodeService);
        mainWindow.Show();
    }
}