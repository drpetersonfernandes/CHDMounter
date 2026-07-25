using SimpleChdDrive.Core.Logging;

namespace SimpleChdDrive.Core.Tests.Logging;

public class ErrorLoggerTests
{
    [Fact]
    public void InitializeGlobalExceptionHandlersDoesNotThrow()
    {
        var exception = Record.Exception(() => ErrorLogger.InitializeGlobalExceptionHandlers());
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorSyncDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.LogErrorSync(new Exception("test"), "test context"));
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(new Exception("test"), "test context", true));
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionWithoutStackTraceDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(new Exception("test"), "test context", false));
        Assert.Null(exception);
    }
}
