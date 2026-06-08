using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using QRCoder.Exceptions;
using QrCodeGenerator.Helpers;
using QrCodeGenerator.Services;
using AppResources = QrCodeGenerator.Properties.Resources;

namespace QrCodeGenerator;

public partial class MainWindow : Window
{
    private readonly IQrCodeService _qrCodeService;
    private readonly AppInstallService _appInstallService = new();
    private readonly DispatcherTimer _debounceTimer;
    private CancellationTokenSource? _generationCts;
    private int _generationSequence;
    private bool _isGenerating;
    private bool _isApplyingLocalization;
    private BitmapSource? _currentQrCode;

    public MainWindow(IQrCodeService qrCodeService)
    {
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));

        InitializeComponent();

        InitializeLanguageSelector();
        ApplyLocalization();
        UpdateInstallVisibility();

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;
    }

    private void InitializeLanguageSelector()
    {
        LanguageComboBox.ItemsSource = LocalizationService.SupportedLanguages;

        var currentCode = LocalizationService.CurrentCulture.TwoLetterISOLanguageName;
        LanguageComboBox.SelectedItem = LocalizationService.SupportedLanguages
            .FirstOrDefault(l => l.Code == currentCode)
            ?? LocalizationService.SupportedLanguages[0];
    }

    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isApplyingLocalization || LanguageComboBox.SelectedItem is not LanguageOption language)
        {
            return;
        }

        if (language.Code == LocalizationService.CurrentCulture.TwoLetterISOLanguageName)
        {
            return;
        }

        LocalizationService.SetCulture(language.Code);
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        _isApplyingLocalization = true;
        try
        {
            Title = AppResources.WindowTitle;
            AppTitleText.Text = AppResources.AppTitle;
            AppSubtitleText.Text = AppResources.AppSubtitle;
            InputLabelText.Text = AppResources.InputLabel;
            LanguageLabelText.Text = AppResources.LanguageLabel;
            InstallLabelText.Text = AppResources.InstallLabel;
            InstallButton.Content = $"📦 {AppResources.InstallButton}";
            PlaceholderTitleText.Text = AppResources.PlaceholderTitle;
            PlaceholderSubtitleText.Text = AppResources.PlaceholderSubtitle;
            LoadingTextBlock.Text = AppResources.LoadingText;
            GenerateButton.Content = AppResources.GenerateButton;
            SaveButton.Content = AppResources.SaveButton;
            ClearButton.Content = AppResources.ClearButton;

            System.Windows.Automation.AutomationProperties.SetName(
                InputTextBox, AppResources.AutomationInputTextBox);
            System.Windows.Automation.AutomationProperties.SetName(
                GenerateButton, AppResources.AutomationGenerateButton);
            System.Windows.Automation.AutomationProperties.SetName(
                SaveButton, AppResources.AutomationSaveButton);
            System.Windows.Automation.AutomationProperties.SetName(
                ClearButton, AppResources.AutomationClearButton);
            System.Windows.Automation.AutomationProperties.SetName(
                QrPreviewImage, AppResources.AutomationQrPreview);
            System.Windows.Automation.AutomationProperties.SetName(
                LanguageComboBox, AppResources.AutomationLanguageSelector);
            System.Windows.Automation.AutomationProperties.SetName(
                InstallButton, AppResources.AutomationInstallButton);

            var trimmedLength = InputTextBox.Text.Trim().Length;
            CharacterCountText.Text = CharacterCountFormatter.Format(trimmedLength);

            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                StatusHintText.Text = AppResources.InputPlaceholder;
            }
            else if (_isGenerating)
            {
                StatusHintText.Text = AppResources.StatusGeneratingOnInput;
            }
            else if (_currentQrCode is not null)
            {
                StatusHintText.Text = AppResources.StatusReady;
            }
        }
        finally
        {
            _isApplyingLocalization = false;
        }
    }

    private void UpdateInstallVisibility()
    {
        InstallPanel.Visibility = _appInstallService.IsInstalled()
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InstallDialog
        {
            Owner = this
        };
        dialog.ApplyLocalization();

        if (dialog.ShowDialog() != true || dialog.SelectedLocation is not InstallLocationType location)
        {
            return;
        }

        RunInstall(location);
    }

    private void RunInstall(InstallLocationType location)
    {
        var result = _appInstallService.Install(location);

        if (result.Success)
        {
            var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? string.Format(AppResources.InstallSuccessMessage, result.InstallDirectory, result.ShortcutPath)
                : string.Format(
                    AppResources.InstallSuccessWithWarningMessage,
                    result.InstallDirectory,
                    result.ShortcutPath,
                    result.ErrorMessage);

            MessageBox.Show(
                message,
                AppResources.InstallSuccessTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            UpdateInstallVisibility();
            return;
        }

        MessageBox.Show(
            string.Format(AppResources.InstallFailedMessage, result.ErrorMessage),
            AppResources.InstallFailedTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var trimmedLength = InputTextBox.Text.Trim().Length;
        CharacterCountText.Text = CharacterCountFormatter.Format(trimmedLength);

        var hasInput = trimmedLength > 0;

        if (!hasInput)
        {
            StopDebounceTimer();
            CancelGeneration();
            ClearPreview();
            StatusHintText.Text = AppResources.InputPlaceholder;
            UpdateButtonStates();
            return;
        }

        StatusHintText.Text = AppResources.StatusGeneratingOnInput;
        SetLoadingState(true);
        StopDebounceTimer();
        _debounceTimer.Start();
        UpdateButtonStates();
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        StopDebounceTimer();
        _ = GenerateQrCodeAsync();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        StopDebounceTimer();
        _ = GenerateQrCodeAsync();
    }

    private async Task GenerateQrCodeAsync()
    {
        StopDebounceTimer();

        var text = InputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            CancelGeneration();
            ClearPreview();
            UpdateButtonStates();
            return;
        }

        CancelGeneration();
        _generationCts = new CancellationTokenSource();
        var token = _generationCts.Token;
        var sequence = ++_generationSequence;

        _isGenerating = true;
        SetLoadingState(true);
        UpdateButtonStates();

        try
        {
            var result = await Task.Run(() => _qrCodeService.GenerateQrCode(text), token);

            if (token.IsCancellationRequested || sequence != _generationSequence)
            {
                return;
            }

            ApplyQrCode(result);
            StatusHintText.Text = AppResources.StatusReady;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer generation request.
        }
        catch (DataTooLongException)
        {
            if (token.IsCancellationRequested || sequence != _generationSequence)
            {
                return;
            }

            ClearPreview();
            StatusHintText.Text = AppResources.StatusDataTooLong;
            MessageBox.Show(
                AppResources.StatusDataTooLong,
                AppResources.ErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested || sequence != _generationSequence)
            {
                return;
            }

            ClearPreview();
            StatusHintText.Text = AppResources.StatusGenerationFailed;
            MessageBox.Show(
                string.Format(AppResources.ErrorGenerationMessage, ex.Message),
                AppResources.ErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (sequence == _generationSequence)
            {
                _isGenerating = false;
                SetLoadingState(false);
                UpdateButtonStates();
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentQrCode is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = AppResources.SaveDialogTitle,
            Filter = AppResources.SaveDialogFilter,
            FileName = "qrcode.png",
            DefaultExt = ".png"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _qrCodeService.SaveAsPng(_currentQrCode, dialog.FileName);
            StatusHintText.Text = string.Format(AppResources.StatusSaved, dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusHintText.Text = AppResources.StatusSaveFailed;
            MessageBox.Show(
                string.Format(AppResources.ErrorSaveMessage, ex.Message),
                AppResources.ErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        StopDebounceTimer();
        CancelGeneration();
        InputTextBox.Clear();
        ClearPreview();
        CharacterCountText.Text = CharacterCountFormatter.Format(0);
        StatusHintText.Text = AppResources.InputPlaceholder;
        UpdateButtonStates();
    }

    private void ApplyQrCode(BitmapSource qrCode)
    {
        _currentQrCode = qrCode;
        QrPreviewImage.Source = qrCode;

        PlaceholderPanel.Visibility = Visibility.Collapsed;
        QrPreviewBorder.Visibility = Visibility.Visible;
    }

    private void ClearPreview()
    {
        _currentQrCode = null;
        QrPreviewImage.Source = null;
        PlaceholderPanel.Visibility = Visibility.Visible;
        QrPreviewBorder.Visibility = Visibility.Collapsed;
        SetLoadingState(false);
    }

    private void SetLoadingState(bool isLoading)
    {
        if (_currentQrCode is null)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            QrPreviewBorder.Opacity = 1.0;
            return;
        }

        LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        QrPreviewBorder.Opacity = isLoading ? 0.45 : 1.0;
    }

    private void UpdateButtonStates()
    {
        var hasInput = !string.IsNullOrWhiteSpace(InputTextBox.Text);
        GenerateButton.IsEnabled = hasInput && !_isGenerating;
        SaveButton.IsEnabled = _currentQrCode is not null && !_isGenerating;
        ClearButton.IsEnabled = hasInput || _currentQrCode is not null;
    }

    private void StopDebounceTimer() => _debounceTimer.Stop();

    private void CancelGeneration()
    {
        if (_generationCts is null)
        {
            return;
        }

        _generationCts.Cancel();
        _generationCts.Dispose();
        _generationCts = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopDebounceTimer();
        _debounceTimer.Tick -= DebounceTimer_Tick;
        CancelGeneration();
        base.OnClosed(e);
    }
}