using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace SimpleChdDrive.Core.Logging;

public class BugReportSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Warning)
            return;

        if (logEvent.Exception != null)
        {
            var context = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendException(logEvent.Exception, context));
        }
        else if (logEvent.Level >= LogEventLevel.Error)
        {
            var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendError(message, null));
        }
        else
        {
            var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendWarning(message));
        }
    }
}
