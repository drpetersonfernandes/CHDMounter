using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace SimpleChdDrive.Core.Services;

public class LoggingService : ILoggingService
{
    private const int MaxEntries = 5000;
    private readonly Dispatcher _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    private string _lastMessage = "";
    private DateTime _lastMessageTime;

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public void Log(string message)
    {
        AppendEntry(message, false);
        Serilog.Log.Information(message);
    }

    public void LogError(string message)
    {
        AppendEntry(message, true);
        Serilog.Log.Error(message);
    }

    private void AppendEntry(string message, bool isError)
    {
        if (_dispatcher.CheckAccess())
        {
            DoAppend(message, isError);
        }
        else
        {
            _dispatcher.InvokeAsync(() => DoAppend(message, isError));
        }
    }

    private void DoAppend(string message, bool isError)
    {
        if (message == _lastMessage && (DateTime.Now - _lastMessageTime).TotalMilliseconds < 100)
            return;

        _lastMessage = message;
        _lastMessageTime = DateTime.Now;

        LogEntries.Add(new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            IsError = isError
        });

        while (LogEntries.Count > MaxEntries)
            LogEntries.RemoveAt(0);
    }
}
