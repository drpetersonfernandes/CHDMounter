using System.Diagnostics;
using SerilogLog = Serilog.Log;

namespace SimpleChdDrive.Core.Logging;

public static class DiagnosticLogger
{
    public static string? LogFilePath { get; private set; }
    public static string AppDataLogFolder { get; private set; } = string.Empty;

    public static string GetAppDataFolder(string appName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
    }

    public static void Initialize(string appName = "SimpleChdDrive")
    {
        AppDataLogFolder = Path.Combine(GetAppDataFolder(appName), "logs");
        Directory.CreateDirectory(AppDataLogFolder);
        LogFilePath = Path.Combine(AppDataLogFolder, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        AppLogger.Initialize(LogFilePath);
    }

    public static void CleanupOldLogs()
    {
        try
        {
            var logDir = AppDataLogFolder;
            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return;

            var oldLogs = Directory.GetFiles(logDir, "debug_*.log");
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

    public static string GetAppDataFolderForCurrentApp()
    {
        return AppDataLogFolder;
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
