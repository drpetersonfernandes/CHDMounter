using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace SimpleCHDDrive;

public partial class App
{
    internal static string[] StartupArgs { get; private set; } = [];

    private static CancellationTokenSource ShutdownCts { get; } = new();

    private static TextWriter? _originalConsoleOut;
    private static TextWriter? _originalConsoleError;
    private static LogTextWriter? _logTextWriter;

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

            RegisterServices();

            _originalConsoleOut = Console.Out;
            _originalConsoleError = Console.Error;
            _logTextWriter = new LogTextWriter(_originalConsoleOut);
            Console.SetOut(_logTextWriter);
            Console.SetError(_logTextWriter);

            var loggingService = ServiceProvider.Get<ILoggingService>();
            loggingService.Log("SimpleChdDrive - CHD Virtual Drive Mounter (Dokan)");
            loggingService.Log("Supports mounting CHD (Compressed Hunks of Data) files as virtual drives");
            loggingService.Log("");
            if (DiagnosticLogger.LogFilePath != null)
            {
                loggingService.Log($"Debug log: {DiagnosticLogger.LogFilePath}");
                loggingService.Log("");
            }

            loggingService.Log("Usage: SimpleChdDrive.exe <chd_file> <console_type> [mount_point]");
            loggingService.Log("Example: SimpleChdDrive.exe game.chd ps2 M");
            loggingService.Log("Example: SimpleChdDrive.exe game.chd xbox N");
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

    private static void RegisterServices()
    {
        var loggingService = new LoggingService();
        ServiceProvider.Register<ILoggingService>(loggingService);

        var settingsService = new SettingsService();
        ServiceProvider.Register<ISettingsService>(settingsService);

        var mountService = new MountService(loggingService, settingsService);
        ServiceProvider.Register<IMountService>(mountService);

        var userNotificationService = new UserNotificationService(loggingService);
        ServiceProvider.Register<IUserNotificationService>(userNotificationService);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DiagnosticLogger.LogSection("APPLICATION SHUTDOWN");
        try
        {
            try { ShutdownCts.Cancel(); } catch (ObjectDisposedException) { }

            try { _logTextWriter?.Dispose(); }
            catch (Exception ex) { ErrorLoggerStatic.ReportSilentException(ex, "App.OnExit: Failed to dispose LogTextWriter", true); }

            try
            {
                if (Current.MainWindow is IDisposable disposableMainWindow)
                    disposableMainWindow.Dispose();
            }
            catch (Exception ex) { ErrorLoggerStatic.ReportSilentException(ex, "App.OnExit: Failed to dispose MainWindow", true); }

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
