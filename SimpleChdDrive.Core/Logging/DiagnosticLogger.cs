using System.Text;

namespace SimpleChdDrive.Core.Logging;

public static class DiagnosticLogger
{
    private static readonly StringBuilder _buffer = new();
    private static string _logFilePath;

    public static string LogFilePath => _logFilePath;

    public static void Initialize()
    {
        _logFilePath = Path.Combine(Path.GetTempPath(), $"SimpleChdDrive_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
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
                catch { }
            }
        }
        catch { }
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
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        _buffer.AppendLine(line);
        System.Diagnostics.Debug.WriteLine($"[DIAG] {message}");

        if (_logFilePath != null)
        {
            try { File.AppendAllText(_logFilePath, line + Environment.NewLine); }
            catch { }
        }
    }
}
