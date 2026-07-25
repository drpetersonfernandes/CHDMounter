using System.Runtime.CompilerServices;
using Serilog;
using SimpleChdDrive.Core.Logging;

namespace SimpleChdDrive.Core.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Debug()
            .WriteTo.Sink(new BugReportSink())
            .CreateLogger();
    }
}
