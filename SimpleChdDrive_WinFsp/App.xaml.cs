using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

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

            DiagnosticLogger.CleanupOldLogs();
            DiagnosticLogger.Initialize();
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

            loggingService.Log("Usage: SimpleChdDrive_WinFsp.exe <chd_file> <console_type> [mount_point]");
            loggingService.Log("Example: SimpleChdDrive_WinFsp.exe game.chd ps2 M");
            loggingService.Log("Example: SimpleChdDrive_WinFsp.exe game.chd xbox N");
            loggingService.Log("Run without args to open the UI and select filesystem type.");
            loggingService.Log("");

            ErrorLoggerStatic.InitializeGlobalExceptionHandlers();

            StatsClient.SendStats();
        }
        catch (Exception ex)
        {
            try
            {
                ErrorLoggerStatic.LogErrorSync(ex, "Critical error during application startup");
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
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (currentPath.Contains("WinFsp", StringComparison.OrdinalIgnoreCase))
                return;

            string? binDir = null;

            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp")
                            ?? Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp");
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
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DiagnosticLogger.LogSection("APPLICATION SHUTDOWN");
        try
        {
            try { _logTextWriter?.Dispose(); }
            catch (Exception ex) { ErrorLoggerStatic.ReportSilentException(ex, "App.OnExit: Failed to dispose LogTextWriter", true); }

            try { ServiceProvider.DisposeAllServices(); }
            catch (Exception ex) { ErrorLoggerStatic.ReportSilentException(ex, "App.OnExit: Failed to dispose services", true); }

            if (_originalConsoleOut != null) Console.SetOut(_originalConsoleOut);
            if (_originalConsoleError != null) Console.SetError(_originalConsoleError);
        }
        catch (Exception ex) { ErrorLoggerStatic.ReportSilentException(ex, "App.OnExit: Error during exit cleanup", true); }

        try { AppLogger.CloseAndFlush(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to flush loggers: {ex.Message}"); }

        base.OnExit(e);
    }
}
