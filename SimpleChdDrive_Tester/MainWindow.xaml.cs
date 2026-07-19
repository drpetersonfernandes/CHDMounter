using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Serilog;
using SimpleChdDrive.Core.Views;
using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;
using Tester.Models;
using Tester.Services;

namespace Tester;

public partial class MainWindow
{
    private readonly ILogger _logger;
    private readonly IScreenshotService _screenshotService;
    private TestRunnerService? _testRunner;
    private TestSummary? _lastSummary;
    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _elapsedTimer;
    private Stopwatch? _stopwatch;

    public MainWindow()
    {
        InitializeComponent();

        _logger = App.Logger ?? new LoggerConfiguration().WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture).CreateLogger();
        _screenshotService = new ScreenshotService(new LoggingService(Dispatcher));

        _elapsedTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _elapsedTimer.Tick += ElapsedTimer_Tick;

        PopulateConsoleTypes();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AppendLog("[Tester] CHD Parsing Test Tool", Colors.Cyan);
        AppendLog("[Tester] Select a folder containing .chd files, choose a console type, and click Run Tests.", Colors.Gray);
        AppendLog("", Colors.Gray);
        _logger.Information("MainWindow loaded");

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
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
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* ignored */ }
        }
    }

    private void UpdateDismiss_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
    }

    private void PopulateConsoleTypes()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        ConsoleComboBox.ItemsSource = consoles;
        ConsoleComboBox.DisplayMemberPath = "Name";
        ConsoleComboBox.SelectedValuePath = "Type";
        ConsoleComboBox.SelectedIndex = 0;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder containing .chd files"
        };

        if (dialog.ShowDialog() == true)
        {
            ChdFolderTextBox.Text = dialog.FolderName;
        }
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPath = ChdFolderTextBox.Text.Trim();
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Please select a valid folder containing .chd files.", "Invalid Folder",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ConsoleComboBox.SelectedItem is not ConsoleInfo consoleInfo)
            {
                MessageBox.Show("Please select a console type.", "Invalid Console",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RunButton.IsEnabled = false;
            ExportPdfButton.Visibility = Visibility.Collapsed;
            SummaryPanel.Visibility = Visibility.Collapsed;

            ClearLog();
            _cts = new CancellationTokenSource();
            _lastSummary = null;

            _testRunner = new TestRunnerService(_logger);
            _testRunner.LogMessage += OnLogMessage;
            _testRunner.AllCompleted += OnAllCompleted;

            _stopwatch = Stopwatch.StartNew();
            _elapsedTimer.Start();

            StatusText.Text = "Running tests...";

            try
            {
                await _testRunner.RunTestsAsync(folderPath, consoleInfo, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("[Cancelled] Test run was cancelled.", Colors.Yellow);
                _logger.Warning("Test run cancelled");
            }
            catch (Exception ex)
            {
                AppendLog($"[Error] {ex.Message}", Colors.Red);
                _logger.Error(ex, "Error during test run");
            }
            finally
            {
                RunButton.IsEnabled = true;
                _elapsedTimer.Stop();
                StatusText.Text = "Ready";
                _stopwatch = null;
                _testRunner.LogMessage -= OnLogMessage;
                _testRunner.AllCompleted -= OnAllCompleted;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[Error] {ex.Message}", Colors.Red);
            _logger.Error(ex, "Error during test run");
        }
    }

    private void OnLogMessage(string message)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (message.StartsWith("  OK", StringComparison.Ordinal))
                AppendLog(message, Colors.Green);
            else if (message.StartsWith("  FAIL", StringComparison.Ordinal))
                AppendLog(message, Colors.Red);
            else if (message.StartsWith(new string('=', 60), StringComparison.Ordinal))
                AppendLog(message, Colors.Cyan);
            else
                AppendLog(message, Colors.LightGray);
        });
    }

    private void OnAllCompleted(TestSummary summary)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _lastSummary = summary;
            ShowSummary(summary);
        });
    }

    private void ShowSummary(TestSummary summary)
    {
        SummaryPanel.Visibility = Visibility.Visible;
        SummaryText.Text = $"Results: {summary.SuccessCount}/{summary.TotalFiles} succeeded";

        SuccessCountText.Text = $"{summary.SuccessCount} OK";
        SuccessBadge.Visibility = summary.SuccessCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        FailCountText.Text = $"{summary.FailCount} FAIL";
        FailBadge.Visibility = summary.FailCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        ExportPdfButton.Visibility = Visibility.Visible;
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSummary is null)
        {
            MessageBox.Show("No test results to export.", "Export PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export Test Summary to PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"CHD_Test_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var exporter = new PdfExportService();
                exporter.ExportToPdf(_lastSummary, dialog.FileName);

                AppendLog($"[Export] Summary exported to: {dialog.FileName}", Colors.Green);
                _logger.Information("Summary exported to PDF: {Path}", dialog.FileName);

                MessageBox.Show($"Report exported successfully to:\n{dialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"[Export Error] {ex.Message}", Colors.Red);
                _logger.Error(ex, "Failed to export PDF");
                MessageBox.Show($"Failed to export PDF: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ElapsedTimer_Tick(object? sender, EventArgs e)
    {
        if (_stopwatch is not null)
        {
            ElapsedText.Text = $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F1}s";
        }
    }

    private void AppendLog(string message, Color color)
    {
        var paragraph = new Paragraph();
        var run = new Run(message + Environment.NewLine)
        {
            Foreground = new SolidColorBrush(color)
        };
        paragraph.Inlines.Add(run);

        LogTextBox.Document.Blocks.Add(paragraph);
        LogTextBox.ScrollToEnd();
    }

    private void ClearLog()
    {
        LogTextBox.Document.Blocks.Clear();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SimpleChdDrive_Tester", "logs");
        if (Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
        else
            AppendLog("[Error] AppData folder not found.", Colors.Red);
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
