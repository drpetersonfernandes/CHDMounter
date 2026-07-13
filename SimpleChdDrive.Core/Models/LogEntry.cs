namespace SimpleChdDrive.Core.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
