using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using SimpleChdDrive.Core.Interfaces;

namespace SimpleChdDrive_WinFsp;

public partial class App
{
    internal static string[] StartupArgs { get; private set; } = [];

    private static TextWriter _originalConsoleOut = null!;
    private static TextWriter _originalConsoleError = null!;
    private static LogTextWriter _logTextWriter = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            StartupArgs = e.Args;

            DiagnosticLogger.Initialize("SimpleChdDrive_WinFsp");
            DiagnosticLogger.CleanupOldLogs();
            DiagnosticLogger.LogSection("APPLICATION STARTUP");
            DiagnosticLogger.Log($"  Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            DiagnosticLogger.Log($"  Arguments: [{string.Join(", ", StartupArgs)}]");
            DiagnosticLogger.Log($"  Base directory: {AppContext.BaseDirectory}");
            DiagnosticLogger.Log($"  OS: {RuntimeInformation.OSDescription}");
            DiagnosticLogger.Log($"  Framework: {RuntimeInformation.FrameworkDescription}");

            EnsureWinFspOnPath();

            RegisterServices();

            _originalConsoleOut = Console.Out;
            _originalConsoleError = Console.Error;
            _logTextWriter = new LogTextWriter(_originalConsoleOut);
            Console.SetOut(_logTextWriter);
            Console.SetError(_logTextWriter);

            var loggingService = ServiceProvider.Get<ILoggingService>();
            loggingService.Log("SimpleChdDrive - CHD Virtual Drive Mounter (WinFsp)");
            loggingService.Log("Supports mounting CHD (Compressed Hunks of Data) files as virtual drives");
            loggingService.Log("");
            if (DiagnosticLogger.LogFilePath != null)
            {
                loggingService.Log($"Debug log: {DiagnosticLogger.LogFilePath}");
                loggingService.Log("");
            }

            loggingService.Log("Usage: SimpleChdDrive_WinFsp.exe <console_type> <chd_file>");
            loggingService.Log("Example: SimpleChdDrive_WinFsp.exe 17 game.chd");
            loggingService.Log("Example: SimpleChdDrive_WinFsp.exe 23 game.chd");
            loggingService.Log("Run without args to open the UI and select filesystem type.");
            loggingService.Log("");

            ErrorLogger.InitializeGlobalExceptionHandlers();

            StatsClient.SendStats();
            UpdateChecker.CheckForUpdates();
        }
        catch (Exception ex)
        {
            try
            {
                ErrorLogger.LogErrorSync(ex, "Critical error during application startup");
            }
            catch
            {
                MessageBox.Show($"Critical startup error: {ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            throw;
        }
    }

    private static void EnsureWinFspOnPath()
    {
        try
        {
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (currentPath.Contains("WinFsp", StringComparison.OrdinalIgnoreCase))
                return;

            string? binDir = null;

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp")
                            ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp");
            var sxsDir = key?.GetValue("SxsDir") as string;
            if (!string.IsNullOrEmpty(sxsDir))
            {
                var sxsBin = Path.Combine(sxsDir, "bin");
                if (Directory.Exists(sxsBin))
                {
                    binDir = sxsBin;
                }
            }

            if (binDir == null)
            {
                var installDir = key?.GetValue("InstallDir") as string;
                if (!string.IsNullOrEmpty(installDir))
                {
                    var installBin = Path.Combine(installDir, "bin");
                    if (Directory.Exists(installBin))
                    {
                        binDir = installBin;
                    }
                }
            }

            if (binDir == null)
                return;

            Environment.SetEnvironmentVariable("PATH", binDir + ";" + currentPath, EnvironmentVariableTarget.Process);
            DiagnosticLogger.Log($"  WinFsp PATH set to: {binDir}");
        }
        catch
        {
            // ignored
        }
    }

    private static void RegisterServices()
    {
        var loggingService = new LoggingService();
        ServiceProvider.Register<ILoggingService>(loggingService);

        var mountService = new MountService(loggingService);
        ServiceProvider.Register<IMountService>(mountService);

        var screenshotService = new ScreenshotService(loggingService);
        ServiceProvider.Register<IScreenshotService>(screenshotService);

        var settingsService = new SettingsService("SimpleChdDrive_WinFsp");
        ServiceProvider.Register<ISettingsService>(settingsService);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DiagnosticLogger.LogSection("APPLICATION SHUTDOWN");
        try
        {
            try
            {
                ServiceProvider.DisposeAllServices();
            }
            catch (Exception ex)
            {
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose services", true);
            }

            Console.SetOut(_originalConsoleOut);
            Console.SetError(_originalConsoleError);

            try
            {
                _logTextWriter.Dispose();
            }
            catch (Exception ex)
            {
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose LogTextWriter", true);
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.ReportSilentException(ex, "App.OnExit: Error during exit cleanup", true);
        }

        try
        {
            AppLogger.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to flush loggers: {ex.Message}");
        }

        base.OnExit(e);
    }
}
