using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SimpleChdDrive.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;

namespace SimpleChdDrive_WinFsp;

internal partial class MainWindow
{
    private readonly ILoggingService _loggingService;
    private readonly IMountService _mountService;
    private readonly IScreenshotService _screenshotService;

    private string _chdPath = null!;
    private ConsoleType _selectedConsoleType = ConsoleType.Unknown;

    public MainWindow()
    {
        InitializeComponent();

        _loggingService = ServiceProvider.Get<ILoggingService>();
        _mountService = ServiceProvider.Get<IMountService>();
        _screenshotService = ServiceProvider.Get<IScreenshotService>();

        PopulateConsoleTypes();
        WireUpLogging();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void WireUpLogging()
    {
        _loggingService.LogEntries.CollectionChanged += (_, e) =>
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
        var args = App.StartupArgs;
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

    private void UpdateBannerButton_Click(object sender, RoutedEventArgs e)
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

    private void UpdateDismiss_Click(object sender, RoutedEventArgs e)
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
                           && (ctFromNumber = ParseConsoleTypeByNumber(consoleNumber)) != null:
                chdPath = args[1];
                break;
            case >= 1 when File.Exists(args[0]):
            {
                chdPath = args[0];
                if (args.Length >= 2)
                {
                    var ct = ParseConsoleType(args[1]);
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

    private static ConsoleType? ParseConsoleTypeByNumber(int number)
    {
        return number switch
        {
            1 => ConsoleType.AmigaCd,
            2 => ConsoleType.AmigaCd32,
            3 => ConsoleType.CDi,
            4 => ConsoleType.GenericIso9660,
            5 => ConsoleType.GenericIsoRaw,
            6 => ConsoleType.GenericCueBin2352Default,
            7 => ConsoleType.GenericCueBin2048,
            8 => ConsoleType.GenericCueIso,
            9 => ConsoleType.GenericCueBinWav,
            10 => ConsoleType.GenericCueIsoWav,
            11 => ConsoleType.Dreamcast,
            12 => ConsoleType.FmTowns,
            13 => ConsoleType.NeoGeoCd,
            14 => ConsoleType.PcEngineCd,
            15 => ConsoleType.PcFx,
            16 => ConsoleType.PlayStation,
            17 => ConsoleType.Ps1,
            18 => ConsoleType.Ps2,
            19 => ConsoleType.Ps3,
            20 => ConsoleType.Psp,
            21 => ConsoleType.Saturn,
            22 => ConsoleType.SegaGenesisCd,
            23 => ConsoleType.ThreeDo,
            24 => ConsoleType.Xbox,
            25 => ConsoleType.Xbox360,
            26 => ConsoleType.X68000,
            27 => ConsoleType.Pico,
            _ => null
        };
    }

    private static ConsoleType ParseConsoleType(string arg)
    {
        return arg.ToLowerInvariant() switch
        {
            "ps1" or "playstation" or "psx" => ConsoleType.Ps1, "psauto" or "psdetect" => ConsoleType.PlayStation, "ps2" => ConsoleType.Ps2, "ps3" => ConsoleType.Ps3,
            "psp" => ConsoleType.Psp, "xbox" => ConsoleType.Xbox, "xbox360" or "x360" => ConsoleType.Xbox360,
            "dreamcast" or "dc" => ConsoleType.Dreamcast,
            "fmtowns" or "fmt" => ConsoleType.FmTowns, "3do" => ConsoleType.ThreeDo,
            "cdi" or "cd-i" => ConsoleType.CDi, "saturn" => ConsoleType.Saturn,
            "neogeo" or "ngcd" => ConsoleType.NeoGeoCd, "pcengine" or "pce" or "tgcd" => ConsoleType.PcEngineCd,
            "pcfx" => ConsoleType.PcFx, "segagenesis" or "megacd" or "segacd" => ConsoleType.SegaGenesisCd,
            "amigacd32" or "cd32" => ConsoleType.AmigaCd32, "amigacd" or "amiga" => ConsoleType.AmigaCd,
            "iso9660" or "generic" or "iso" => ConsoleType.GenericIso9660, "cuebin" or "cue" => ConsoleType.GenericCueBin2352Default,
            "cuebin2048" or "cue2048" => ConsoleType.GenericCueBin2048,
            "cueiso" => ConsoleType.GenericCueIso,
            "cuebinwav" or "cuewav" => ConsoleType.GenericCueBinWav,
            "cueisowav" => ConsoleType.GenericCueIsoWav,
            "x68000" or "x68k" => ConsoleType.X68000,
            "pico" => ConsoleType.Pico,
            _ => ConsoleType.Unknown
        };
    }

    private void ValidateAndEnableMount()
    {
        var type = ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci ? ci.Type : _selectedConsoleType;
        MountButton.IsEnabled = !string.IsNullOrEmpty(_chdPath) && type != ConsoleType.Unknown && File.Exists(_chdPath) && !_mountService.IsMounted;
    }

    private void ConsoleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci)
        {
            _selectedConsoleType = ci.Type;
        }

        ValidateAndEnableMount();
    }

    private void ChdFilePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        _chdPath = ChdFilePathTextBox.Text.Trim().Trim('"');
        ValidateAndEnableMount();
    }

    private void BrowseChd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "CHD files (*.chd)|*.chd|All files (*.*)|*.*", Title = "Select CHD File" };
        if (dlg.ShowDialog() == true)
        {
            ChdFilePathTextBox.Text = dlg.FileName;
            _chdPath = dlg.FileName;
            ValidateAndEnableMount();
        }
    }

    private void OpenChd_Click(object sender, RoutedEventArgs e)
    {
        BrowseChd_Click(sender, e);
    }

    private async void Mount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await MountDiskAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Mount failed: {ex.Message}");
        }
    }

    private void MountDisk()
    {
        _ = MountDiskAsync();
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

            await Task.Run(() => _mountService.Mount(_chdPath, null, type));
            if (_mountService.IsMounted)
            {
                StatusText.Text = "Mounted";
                DriveLetterText.Text = _mountService.MountPoint;
                UnmountButton.IsEnabled = true;
                try
                {
                    var settings = ServiceProvider.TryGet<ISettingsService>();
                    if (settings.Settings.AutoOpenMountedDrive)
                        Process.Start("explorer.exe", _mountService.MountPoint);
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
            _loggingService.LogError($"Mount failed: {ex.Message}");
            StatusText.Text = "Mount failed";
            MountButton.IsEnabled = true;
        }
    }

    private async void Unmount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UnmountButton.IsEnabled = false;
            StatusText.Text = "Unmounting...";

            try
            {
                await Task.Run(() => _mountService.Unmount());
                StatusText.Text = "Unmounted";
                DriveLetterText.Text = "";
                MountButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Unmount failed: {ex.Message}");
                StatusText.Text = "Unmount failed";
                UnmountButton.IsEnabled = _mountService.IsMounted;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Unmount failed: {ex.Message}");
            StatusText.Text = "Unmount failed";
            UnmountButton.IsEnabled = _mountService.IsMounted;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = DiagnosticLogger.GetAppDataFolderForCurrentApp();
        if (Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
        else
            _loggingService.LogError($"AppData folder not found: {folder}");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = ServiceProvider.TryGet<ISettingsService>();
        new SettingsWindow(settingsService) { Owner = this }.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            _mountService.Unmount();
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
