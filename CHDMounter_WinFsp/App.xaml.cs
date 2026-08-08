using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using CHDMounter.Core.Interfaces;

namespace CHDMounter_WinFsp;

/// <summary>
/// Application entry point for the WinFsp-based CHD mounter. Handles service registration,
/// logging initialization, WinFsp path configuration, and application lifecycle.
/// </summary>
public partial class App
{
    internal static string[] StartupArgs { get; private set; } = [];

    private static TextWriter _originalConsoleOut = null!;
    private static TextWriter _originalConsoleError = null!;
    private static LogTextWriter _logTextWriter = null!;

    /// <summary>
    /// Handles application startup: captures command-line arguments, initializes logging,
    /// configures global exception handlers, and registers shared services.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            StartupArgs = e.Args;

            DiagnosticLogger.Initialize("CHDMounter_WinFsp");
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
            loggingService.Log("CHDMounter - CHD Virtual Drive Mounter (WinFsp)");
            loggingService.Log("Supports mounting CHD (Compressed Hunks of Data) files as virtual drives");
            loggingService.Log("");
            if (DiagnosticLogger.LogFilePath is not null)
            {
                loggingService.Log($"Debug log: {DiagnosticLogger.LogFilePath}");
                loggingService.Log("");
            }

            loggingService.Log("Usage: CHDMounter_WinFsp.exe [/l] [/a] [/s:<alias>] <chd_file> [mount_point]");
            loggingService.Log("Example: CHDMounter_WinFsp.exe /s:ps2 game.chd");
            loggingService.Log("Example: CHDMounter_WinFsp.exe /l /s:segadreamcast game.chd M:");
            loggingService.Log("Console aliases: see the Console Type Reference in the README (e.g. neogeocd, cuebin2352, cueisowav2352).");
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
                Serilog.Log.Error(ex, "Critical error during application startup");
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

            if (binDir is null)
                return;

            Environment.SetEnvironmentVariable("PATH", binDir + ";" + currentPath, EnvironmentVariableTarget.Process);
            DiagnosticLogger.Log($"  WinFsp PATH set to: {binDir}");
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to configure WinFsp PATH");
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

        var settingsService = new SettingsService("CHDMounter_WinFsp");
        ServiceProvider.Register<ISettingsService>(settingsService);
    }

    /// <summary>
    /// Handles application shutdown: logs the shutdown section, flushes logging output,
    /// and disposes all registered services.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
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
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose services");
            }

            Console.SetOut(_originalConsoleOut);
            Console.SetError(_originalConsoleError);

            try
            {
                _logTextWriter.Dispose();
            }
            catch (Exception ex)
            {
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose LogTextWriter");
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.ReportSilentException(ex, "App.OnExit: Error during exit cleanup");
        }

        try
        {
            AppLogger.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to flush loggers during shutdown");
        }

        base.OnExit(e);
    }
}
