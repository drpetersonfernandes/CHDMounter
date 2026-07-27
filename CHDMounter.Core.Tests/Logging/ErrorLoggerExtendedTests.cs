using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class ErrorLoggerExtendedTests
{
    [Fact]
    public void LogErrorSyncWithInnerExceptionDoesNotThrow()
    {
        var inner = new ArgumentException("inner error");
        var outer = new InvalidOperationException("outer error", inner);
        var exception = Record.Exception(() => ErrorLogger.LogErrorSync(outer, "test context"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorSyncWithEmptyContextDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.LogErrorSync(new Exception("test"), ""));
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorSyncWithNullExceptionMessageDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.LogErrorSync(new Exception(), "context"));
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionDoesNotThrow()
    {
        Exception ex;
        try
        {
            throw new InvalidOperationException("test");
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(ex, "context"));
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionWithEmptyContextDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(new Exception("test"), ""));
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionWithNullExceptionMessageDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(new Exception(), "context"));
        Assert.Null(exception);
    }

    [Fact]
    public void InitializeGlobalExceptionHandlersCanBeCalledTwice()
    {
        // Should not throw even if called multiple times
        ErrorLogger.InitializeGlobalExceptionHandlers();
        var exception = Record.Exception(() => ErrorLogger.InitializeGlobalExceptionHandlers());
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorSyncWithComplexExceptionHierarchy()
    {
        Exception ex;
        try
        {
            try
            {
                throw new ArgumentException("deep error");
            }
            catch (Exception inner)
            {
                throw new AggregateException("aggregate", inner);
            }
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        var exception = Record.Exception(() => ErrorLogger.LogErrorSync(ex, "complex context"));
        Assert.Null(exception);
    }
}
