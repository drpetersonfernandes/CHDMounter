using System.Diagnostics;
using SerilogLog = Serilog.Log;

namespace SimpleChdDrive.Core.Logging;

public static class DiagnosticLogger
{
    public static string? LogFilePath { get; private set; }

    public static void Initialize()
    {
        LogFilePath = Path.Combine(Path.GetTempPath(), $"SimpleChdDrive_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        AppLogger.Initialize(LogFilePath);
    }

    public static void CleanupOldLogs()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var oldLogs = Directory.GetFiles(tempPath, "SimpleChdDrive_Debug_*.log");
            foreach (var log in oldLogs)
            {
                try
                {
                    var fi = new FileInfo(log);
                    if (fi.CreationTime < DateTime.Now.AddDays(-7))
                        File.Delete(log);
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    public static void LogSection(string section)
    {
        var line = new string('=', 60);
        Log(line);
        Log($"  {section}");
        Log(line);
    }

    public static void Log(string message)
    {
        Debug.WriteLine($"[DIAG] {message}");
        SerilogLog.Debug(message);
    }
}
