using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

    /// <summary>
    /// Returns the startup command-line arguments. Override in derived classes to supply application-specific arguments.
    /// </summary>
    /// <returns>An array of command-line argument strings.</returns>
    protected virtual string[] GetStartupArgs()
    {
        return [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowBase"/> class and resolves required services.
    /// </summary>
    public MainWindowBase()
    {
        LoggingService = ServiceProvider.Get<ILoggingService>();
        MountService = ServiceProvider.Get<IMountService>();
        _screenshotService = ServiceProvider.Get<IScreenshotService>();
    }

    /// <summary>
    /// Initializes the main window by populating console types, wiring up logging, and registering event handlers.
    /// Called from derived class constructors.
    /// </summary>
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
                foreach (LogEntry entry in e.NewItems!)
                    LogTextBox.AppendText($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}{Environment.NewLine}");
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

    /// <summary>
    /// Handles clicks on the update banner button by opening the download URL in the default browser.
    /// </summary>
    protected void UpdateBannerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to open update URL: {Url}", url);
            }
        }
    }

    /// <summary>
    /// Handles clicks on the update dismiss button by collapsing the update banner.
    /// </summary>
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
            case >= 2 when int.TryParse(args[0], CultureInfo.InvariantCulture, out var consoleNumber)
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

        if (chdPath is not null)
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

        if (ctFromNumber.HasValue && chdPath is not null && File.Exists(chdPath))
        {
            MountDisk();
        }
        else if (chdPath is not null && File.Exists(chdPath) && !ctFromNumber.HasValue)
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

    /// <summary>
    /// Handles selection changes in the console type combo box.
    /// </summary>
    protected void ConsoleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci)
        {
            _selectedConsoleType = ci.Type;
        }

        ValidateAndEnableMount();
    }

    /// <summary>
    /// Handles text changes in the CHD file path text box.
    /// </summary>
    protected void ChdFilePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        _chdPath = ChdFilePathTextBox.Text.Trim().Trim('"');
        ValidateAndEnableMount();
    }

    /// <summary>
    /// Opens a file dialog to browse for a CHD file.
    /// </summary>
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

    /// <summary>
    /// Opens a CHD file via the file menu.
    /// </summary>
    protected void OpenChd_Click(object sender, RoutedEventArgs e)
    {
        BrowseChd_Click(sender, e);
    }

    /// <summary>
    /// Handles the mount button click by initiating an asynchronous mount operation.
    /// </summary>
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

        if (!MountService.CanMount())
        {
            StatusText.Text = "Driver not found";
            return;
        }

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
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "Failed to auto-open mounted drive in explorer");
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

    /// <summary>
    /// Handles the unmount button click by initiating an asynchronous unmount operation.
    /// </summary>
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

    /// <summary>
    /// Closes the application window.
    /// </summary>
    protected void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Opens the application data folder in File Explorer.
    /// </summary>
    protected void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = DiagnosticLogger.GetAppDataFolderForCurrentApp();
        if (Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
        else
            LoggingService.LogError($"AppData folder not found: {folder}");
    }

    /// <summary>
    /// Opens the settings dialog.
    /// </summary>
    protected void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = ServiceProvider.TryGet<ISettingsService>();
        new SettingsWindow(settingsService) { Owner = this }.ShowDialog();
    }

    /// <summary>
    /// Opens the about dialog.
    /// </summary>
    protected void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private bool _isClosing;

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            if (_isClosing) return;

            if (!MountService.IsMounted) return;

            e.Cancel = true;
            _isClosing = true;
            StatusText.Text = "Unmounting before exit...";

            try
            {
                await Task.Run(() => MountService.Unmount());
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to unmount during window close");
            }

            _isClosing = false;
            Close();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error during window closing");
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
