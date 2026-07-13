using System.Collections.ObjectModel;
using SimpleChdDrive.Core.Models;

namespace SimpleChdDrive.Core.Services;

public interface ILoggingService
{
    ObservableCollection<LogEntry> LogEntries { get; }
    void Log(string message);
    void LogError(string message);
}
