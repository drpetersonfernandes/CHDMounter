using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;

namespace SimpleCHDDrive;

public partial class MainWindow : Window
{
    private readonly ILoggingService _loggingService;
    private readonly IMountService _mountService;
    private readonly IUserNotificationService _userNotificationService;

    private string _chdPath;
    private ConsoleType _selectedConsoleType = ConsoleType.Unknown;

    public MainWindow()
    {
        InitializeComponent();

        _loggingService = ServiceProvider.Get<ILoggingService>();
        _mountService = ServiceProvider.Get<IMountService>();
        _userNotificationService = ServiceProvider.Get<IUserNotificationService>();

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
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}");
                LogTextBox.Text = sb.ToString();
                LogTextBox.ScrollToEnd();
            });
        };
    }

    private void PopulateConsoleTypes()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        ConsoleTypeComboBox.ItemsSource = consoles;
        ConsoleTypeComboBox.DisplayMemberPath = "Name";
        ConsoleTypeComboBox.SelectedIndex = 0;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var args = App.StartupArgs;
        if (args.Length > 0)
            HandleCommandLineArgs(args);
        else
            ShowConsoleSelectionDialog();
    }

    private void ShowConsoleSelectionDialog()
    {
        var dialog = new ConsoleSelectionWindow { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedConsoleType != ConsoleType.Unknown)
        {
            _selectedConsoleType = dialog.SelectedConsoleType;
            SelectConsoleTypeInCombo(_selectedConsoleType);
            ValidateAndEnableMount();
        }
    }

    private void HandleCommandLineArgs(string[] args)
    {
        if (args.Length >= 1 && File.Exists(args[0]))
        {
            ChdFilePathTextBox.Text = args[0];
            _chdPath = args[0];

            if (args.Length >= 2)
            {
                var ct = ParseConsoleType(args[1]);
                if (ct != ConsoleType.Unknown)
                {
                    _selectedConsoleType = ct;
                    SelectConsoleTypeInCombo(ct);
                }
            }

            ValidateAndEnableMount();

            if (args.Length >= 1 && _selectedConsoleType != ConsoleType.Unknown)
                MountDisk();
        }
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

    private static ConsoleType ParseConsoleType(string arg)
    {
        return arg.ToLowerInvariant() switch
        {
            "ps1" or "playstation" or "psx" => ConsoleType.PS1,
            "ps2" => ConsoleType.PS2,
            "ps3" => ConsoleType.PS3,
            "psp" => ConsoleType.PSP,
            "xbox" => ConsoleType.Xbox,
            "xbox360" or "x360" => ConsoleType.Xbox360,
            "dreamcast" or "dc" => ConsoleType.Dreamcast,
            "3do" => ConsoleType.ThreeDO,
            "cdi" or "cd-i" => ConsoleType.CDi,
            "saturn" => ConsoleType.Saturn,
            "neogeo" or "ngcd" => ConsoleType.NeoGeoCD,
            "pcengine" or "pce" or "tgcd" => ConsoleType.PcEngineCD,
            "pcfx" => ConsoleType.PcFx,
            "segagenesis" or "megacd" or "segacd" => ConsoleType.SegaGenesisCD,
            "amigacd32" or "amiga" => ConsoleType.AmigaCD32,
            "amigacd" => ConsoleType.AmigaCD,
            "iso9660" or "generic" or "iso" => ConsoleType.GenericISO9660,
            "cuebin" or "cue" => ConsoleType.GenericCueBin,
            _ => ConsoleType.Unknown
        };
    }

    private void ValidateAndEnableMount()
    {
        MountButton.IsEnabled = !string.IsNullOrEmpty(_chdPath)
                                && _selectedConsoleType != ConsoleType.Unknown
                                && File.Exists(_chdPath);
    }

    private void BrowseChd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
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

    private void OpenChd_Click(object sender, RoutedEventArgs e) => BrowseChd_Click(sender, e);

    private async void Mount_Click(object sender, RoutedEventArgs e) => await MountDiskAsync();

    private void MountDisk() => _ = MountDiskAsync();

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
                type = sci.Type;

            await Task.Run(() => _mountService.Mount(_chdPath!, null, type));

            if (_mountService.IsMounted)
            {
                StatusText.Text = "Mounted";
                DriveLetterText.Text = _mountService.MountPoint ?? "";
                UnmountButton.IsEnabled = true;
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

    private void Unmount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _mountService.Unmount();
            StatusText.Text = "Unmounted";
            DriveLetterText.Text = "";
            MountButton.IsEnabled = true;
            UnmountButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Unmount failed: {ex.Message}");
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try { _mountService.Unmount(); } catch { }
    }
}
