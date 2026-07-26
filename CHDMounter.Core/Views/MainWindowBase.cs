using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Views;

/// <summary>
/// Shared base class for both Dokan and WinFsp MainWindow implementations.
/// Contains all common UI logic, command-line handling, mount/unmount flow, and update checking.
/// </summary>
public class MainWindowBase : Window
{
    private readonly IScreenshotService _screenshotService;

    private string? _chdPath;
    private ConsoleType _selectedConsoleType = ConsoleType.Unknown;

    private ILoggingService LoggingService { get; }

    private IMountService MountService { get; }

    private TextBox LogTextBox => (TextBox)FindName("LogTextBox")!;
    private TextBox ChdFilePathTextBox => (TextBox)FindName("ChdFilePathTextBox")!;
    private ComboBox ConsoleTypeComboBox => (ComboBox)FindName("ConsoleTypeComboBox")!;
    private Button MountButton => (Button)FindName("MountButton")!;
    private Button UnmountButton => (Button)FindName("UnmountButton")!;
    private TextBlock StatusText => (TextBlock)FindName("StatusText")!;
    private TextBlock DriveLetterText => (TextBlock)FindName("DriveLetterText")!;
    private Border UpdateBanner => (Border)FindName("UpdateBanner")!;
    private TextBlock UpdateBannerText => (TextBlock)FindName("UpdateBannerText")!;
    private Button UpdateBannerButton => (Button)FindName("UpdateBannerButton")!;

    protected virtual string[] GetStartupArgs()
    {
        return [];
    }

    public MainWindowBase()
    {
        LoggingService = ServiceProvider.Get<ILoggingService>();
        MountService = ServiceProvider.Get<IMountService>();
        _screenshotService = ServiceProvider.Get<IScreenshotService>();
    }

    protected void InitializeMainWindow()
    {
        PopulateConsoleTypes();
        WireUpLogging();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        KeyDown += MainWindow_KeyDown;
    }

    private void WireUpLogging()
    {
        LoggingService.LogEntries.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;

            Dispatcher.InvokeAsync(() =>
            {
                var sb = new StringBuilder(LogTextBox.Text);
                foreach (LogEntry entry in e.NewItems!)
                    sb.AppendLine(CultureInfo.InvariantCulture, $"[{entry.Timestamp:HH:mm:ss}] {entry.Message}");
                LogTextBox.Text = sb.ToString();
                LogTextBox.ScrollToEnd();
            });
        };
    }

    private void PopulateConsoleTypes()
    {
        var consoles = new List<ConsoleInfo> { new(ConsoleType.Unknown, "Unknown") };
        consoles.AddRange(ParserFactory.GetAllSupportedConsoles());
        ConsoleTypeComboBox.ItemsSource = consoles;
        ConsoleTypeComboBox.SelectedIndex = 0;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var args = GetStartupArgs();
        if (args.Length > 0)
            HandleCommandLineArgs(args);

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var timer = new System.Windows.Threading.DispatcherTimer(
            System.Windows.Threading.DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var result = UpdateChecker.Result;
            if (result is { HasUpdate: true })
            {
                UpdateBanner.Visibility = Visibility.Visible;
                UpdateBannerText.Text = $"A new version ({result.LatestVersion}) is available!";
                UpdateBannerButton.Tag = result.DownloadUrl;
            }
        };
        timer.Start();
    }

    protected void UpdateBannerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                /* ignored */
            }
        }
    }

    protected void UpdateDismiss_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
    }

    private void HandleCommandLineArgs(string[] args)
    {
        ConsoleType? ctFromNumber = null;
        string? chdPath = null;

        switch (args.Length)
        {
            case >= 2 when int.TryParse(args[0], out var consoleNumber)
                           && (ctFromNumber = ConsoleTypeHelper.ParseByNumber(consoleNumber)) != null:
                chdPath = args[1];
                break;
            case >= 1 when File.Exists(args[0]):
            {
                chdPath = args[0];
                if (args.Length >= 2)
                {
                    var ct = ConsoleTypeHelper.ParseByName(args[1]);
                    if (ct != ConsoleType.Unknown)
                    {
                        ctFromNumber = ct;
                    }
                }

                break;
            }
        }

        if (chdPath != null)
        {
            ChdFilePathTextBox.Text = chdPath;
            _chdPath = chdPath;
        }

        if (ctFromNumber.HasValue)
        {
            _selectedConsoleType = ctFromNumber.Value;
            SelectConsoleTypeInCombo(ctFromNumber.Value);
        }

        ValidateAndEnableMount();

        if (ctFromNumber.HasValue && chdPath != null && File.Exists(chdPath))
        {
            MountDisk();
        }
        else if (chdPath != null && File.Exists(chdPath) && !ctFromNumber.HasValue)
        {
            ShowDragDropConsoleModal(chdPath);
        }
    }

    private void ShowDragDropConsoleModal(string chdPath)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var dialog = new ConsoleSelectionWindow(chdPath) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _selectedConsoleType = dialog.SelectedConsoleType;
                SelectConsoleTypeInCombo(dialog.SelectedConsoleType);
                MountDisk();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void SelectConsoleTypeInCombo(ConsoleType type)
    {
        foreach (var item in ConsoleTypeComboBox.Items)
        {
            if (item is ConsoleInfo ci && ci.Type == type)
            {
                ConsoleTypeComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void ValidateAndEnableMount()
    {
        var type = ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci ? ci.Type : _selectedConsoleType;
        MountButton.IsEnabled = !string.IsNullOrEmpty(_chdPath)
                                && type != ConsoleType.Unknown
                                && File.Exists(_chdPath)
                                && !MountService.IsMounted;
    }

    protected void ConsoleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci)
        {
            _selectedConsoleType = ci.Type;
        }

        ValidateAndEnableMount();
    }

    protected void ChdFilePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        _chdPath = ChdFilePathTextBox.Text.Trim().Trim('"');
        ValidateAndEnableMount();
    }

    protected void BrowseChd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CHD files (*.chd)|*.chd|All files (*.*)|*.*",
            Title = "Select CHD File"
        };
        if (dialog.ShowDialog() == true)
        {
            ChdFilePathTextBox.Text = dialog.FileName;
            _chdPath = dialog.FileName;
            ValidateAndEnableMount();
        }
    }

    protected void OpenChd_Click(object sender, RoutedEventArgs e)
    {
        BrowseChd_Click(sender, e);
    }

    protected async void Mount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await MountDiskAsync();
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Mount failed: {ex.Message}");
        }
    }

    private void MountDisk()
    {
        _ = MountDiskAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                LoggingService.LogError($"Mount failed: {t.Exception?.InnerException?.Message}");
        }, TaskScheduler.Default);
    }

    private async Task MountDiskAsync()
    {
        if (string.IsNullOrEmpty(_chdPath)) return;

        MountButton.IsEnabled = false;
        UnmountButton.IsEnabled = false;
        StatusText.Text = "Mounting...";

        try
        {
            var type = _selectedConsoleType;
            if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo sci)
            {
                type = sci.Type;
            }

            await Task.Run(() => MountService.Mount(_chdPath, null, type));

            if (MountService.IsMounted)
            {
                StatusText.Text = "Mounted";
                DriveLetterText.Text = MountService.MountPoint;
                UnmountButton.IsEnabled = true;

                try
                {
                    var settings = ServiceProvider.TryGet<ISettingsService>();
                    if (settings is { Settings.AutoOpenMountedDrive: true })
                        Process.Start("explorer.exe", MountService.MountPoint);
                }
                catch
                {
                    /* ignored */
                }
            }
            else
            {
                StatusText.Text = "Mount failed";
                MountButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Mount failed: {ex.Message}");
            StatusText.Text = "Mount failed";
            MountButton.IsEnabled = true;
        }
    }

    protected async void Unmount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UnmountButton.IsEnabled = false;
            StatusText.Text = "Unmounting...";

            try
            {
                await Task.Run(() => MountService.Unmount());
                StatusText.Text = "Unmounted";
                DriveLetterText.Text = "";
                MountButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Unmount failed: {ex.Message}");
                StatusText.Text = "Unmount failed";
                UnmountButton.IsEnabled = MountService.IsMounted;
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Unmount failed: {ex.Message}");
            StatusText.Text = "Unmount failed";
            UnmountButton.IsEnabled = MountService.IsMounted;
        }
    }

    protected void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = DiagnosticLogger.GetAppDataFolderForCurrentApp();
        if (Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
        else
            LoggingService.LogError($"AppData folder not found: {folder}");
    }

    protected void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = ServiceProvider.TryGet<ISettingsService>();
        new SettingsWindow(settingsService) { Owner = this }.ShowDialog();
    }

    protected void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            MountService.Unmount();
        }
        catch
        {
            // ignored
        }
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F8)
        {
            _screenshotService.TakeScreenshot();
            e.Handled = true;
        }
    }
}
