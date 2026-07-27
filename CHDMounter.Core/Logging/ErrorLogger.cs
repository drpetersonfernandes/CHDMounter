using Serilog;

namespace CHDMounter.Core.Logging;

/// <summary>
/// Provides global exception handling and centralized error logging for unhandled exceptions.
/// </summary>
public static class ErrorLogger
{
    /// <summary>
    /// Registers global exception handlers for <see cref="AppDomain.UnhandledException"/>
    /// and <see cref="TaskScheduler.UnobservedTaskException"/>.
    /// </summary>
    public static void InitializeGlobalExceptionHandlers()
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

    /// <summary>
    /// Synchronously logs an exception with the specified context to both the diagnostic logger and Serilog.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="context">A description of the context in which the exception occurred.</param>
    public static void LogErrorSync(Exception ex, string context)
    {
        try
        {
            Log.Error(ex, "{Context}", context);
        }
        catch
        {
            // Last resort - cannot log
        }
    }

    /// <summary>
    /// Logs an exception silently to the diagnostic logger without raising it to the user.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="context">A description of the context in which the exception occurred.</param>
    public static void ReportSilentException(Exception ex, string context)
    {
        try
        {
            Log.Warning(ex, "{Context}", context);
        }
        catch
        {
            // Last resort - cannot log
        }
    }
}
