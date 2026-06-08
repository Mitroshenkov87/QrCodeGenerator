using System.Windows;
using QrCodeGenerator.Services;
using AppResources = QrCodeGenerator.Properties.Resources;

namespace QrCodeGenerator;

public partial class InstallDialog : Window
{
    public InstallLocationType? SelectedLocation { get; private set; }

    public InstallDialog()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        Title = AppResources.InstallDialogTitle;
        DialogTitleText.Text = AppResources.InstallDialogTitle;
        DialogDescriptionText.Text = AppResources.InstallDialogDescription;
        LocalOptionTitleText.Text = AppResources.InstallOptionLocalTitle;
        LocalOptionDescriptionText.Text = AppResources.InstallOptionLocalDescription;
        AllUsersOptionTitleText.Text = AppResources.InstallOptionAllUsersTitle;
        AllUsersOptionDescriptionText.Text = AppResources.InstallOptionAllUsersDescription;
        CancelButton.Content = AppResources.InstallDialogCancel;
        ConfirmButton.Content = AppResources.InstallDialogConfirm;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLocation = LocalOption.IsChecked == true
            ? InstallLocationType.Local
            : InstallLocationType.AllUsers;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}