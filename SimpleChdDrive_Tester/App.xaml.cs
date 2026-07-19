using System.Globalization;
using System.Windows;
using Serilog;

namespace Tester;

public partial class App
{
    internal static ILogger? Logger { get; private set; }
    internal static string LogFilePath { get; private set; } = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        LogFilePath = Path.Combine(logDir, $"tester_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(LogFilePath,
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Infinite,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        Logger = loggerConfig.CreateLogger();
        Log.Logger = Logger;

        Logger.Information("Tester application starting");
        Logger.Information("Log file: {LogFilePath}", LogFilePath);

        StatsClient.SendStats();

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Fatal(args.Exception, "Unhandled UI exception");
            MessageBox.Show($"Unhandled error: {args.Exception.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Logger.Fatal(ex, "Unhandled domain exception");
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger?.Information("Tester application shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
