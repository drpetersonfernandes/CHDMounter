using System.Text;

namespace SimpleChdDrive.Core.Logging;

public class LogTextWriter : TextWriter
{
    private readonly TextWriter _originalWriter;

    public override Encoding Encoding => Encoding.UTF8;

    public LogTextWriter(TextWriter originalWriter)
    {
        _originalWriter = originalWriter;
    }

    public override void Write(char value)
    {
        _originalWriter.Write(value);
    }

    public override void WriteLine(string value)
    {
        _originalWriter.WriteLine(value);
        try
        {
            var loggingService = ServiceProvider.TryGet<ILoggingService>();
            if (value != null)
                loggingService?.Log(value);
        }
        catch { }
    }
}
