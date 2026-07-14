using Serilog;

namespace SimpleChdDrive.Core.Logging;

public static class ErrorLoggerStatic
{
    private static ErrorLogger? _instance;

    public static ErrorLogger Instance
    {
        get
        {
            _instance ??= new ErrorLogger();
            return _instance;
        }
    }

    public static void InitializeGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogErrorSync(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogErrorSync(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    public static void LogErrorSync(Exception ex, string context)
    {
        try
        {
            DiagnosticLogger.Log($"ERROR [{context}]: {ex.Message}");
            DiagnosticLogger.Log($"  Stack: {ex.StackTrace}");
            Log.Error(ex, context);
        }
        catch
        {
            // ignored
        }
    }

    public static void ReportSilentException(Exception ex, string context, bool includeStackTrace)
    {
        try
        {
            DiagnosticLogger.Log($"SILENT [{context}]: {ex.Message}");
            if (includeStackTrace)
                DiagnosticLogger.Log($"  Stack: {ex.StackTrace}");
        }
        catch
        {
            // ignored
        }
    }
}

public class ErrorLogger : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
