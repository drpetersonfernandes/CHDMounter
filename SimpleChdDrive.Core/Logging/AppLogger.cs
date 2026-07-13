using Serilog;
using Serilog.Events;

namespace SimpleChdDrive.Core.Logging;

public static class AppLogger
{
    private static ILogger _logger;

    public static void Initialize(string logFilePath)
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        Log.Logger = _logger;
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
