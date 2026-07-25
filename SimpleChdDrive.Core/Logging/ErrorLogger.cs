using Serilog;

namespace SimpleChdDrive.Core.Logging;

/// <summary>
/// Provides global exception handling and centralized error logging for unhandled exceptions.
/// </summary>
public static class ErrorLogger
{
    internal static void InitializeGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += static (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogErrorSync(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            LogErrorSync(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    internal static void LogErrorSync(Exception ex, string context)
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

    internal static void ReportSilentException(Exception ex, string context, bool includeStackTrace)
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
