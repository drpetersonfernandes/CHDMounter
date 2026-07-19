using System.Collections.ObjectModel;

namespace SimpleChdDrive.Core.Interfaces;

public interface ILoggingService
{
    ObservableCollection<LogEntry> LogEntries { get; }
    void Log(string message);
    void LogError(string message);
}
