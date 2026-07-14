using Serilog;

namespace SimpleChdDrive.Core.Logging;

public static class ErrorLoggerStatic
{
    public static void InitializeGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogErrorSync(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
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
