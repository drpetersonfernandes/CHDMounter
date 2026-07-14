using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SimpleChdDrive.Core.Services;

public static class BugReportClient
{
    private const string BaseUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private const string ApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private static readonly HttpClient Client = new();
    private static readonly ConcurrentQueue<Func<Task>> PendingReports = new();
    private static int _isProcessing;

    public static void SendException(Exception ex, string context)
    {
        var envDetails = BuildEnvironmentDetails();
        var errorDetails = $"{context}: {ex.Message}";
        var exceptionDetails = BuildExceptionDetails(ex);

        var message = $"""
            === Environment Details ===
            {envDetails}

            === Error Details ===
            {errorDetails}

            === Exception Details ===
            {exceptionDetails}
            """;

        Enqueue(message, ex.StackTrace ?? "");
    }

    public static void SendWarning(string message)
    {
        var envDetails = BuildEnvironmentDetails();

        var formatted = $"""
            === Environment Details ===
            {envDetails}

            === Warning Details ===
            {message}
            """;

        Enqueue(formatted, "");
    }

    public static void SendError(string message, string? stackTrace)
    {
        var envDetails = BuildEnvironmentDetails();

        var formatted = $"""
            === Environment Details ===
            {envDetails}

            === Error Details ===
            {message}
            """;

        Enqueue(formatted, stackTrace ?? "");
    }

    private static void Enqueue(string message, string stackTrace)
    {
        PendingReports.Enqueue(() => SendAsync(message, stackTrace));
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
        {
            _ = Task.Run(ProcessQueue);
        }
    }

    private static async Task ProcessQueue()
    {
        try
        {
            while (PendingReports.TryDequeue(out var sendAction))
            {
                try { await sendAction(); }
                catch { }
                await Task.Delay(6000);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
            if (!PendingReports.IsEmpty && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                _ = Task.Run(ProcessQueue);
            }
        }
    }

    private static async Task SendAsync(string message, string stackTrace)
    {
        try
        {
            var payload = new
            {
                message = Truncate(message, 4000),
                applicationName = GetAppName(),
                version = GetVersion(),
                environment = "Production",
                stackTrace = Truncate(stackTrace, 8000)
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = content
            };
            request.Headers.Add("X-API-KEY", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Client.SendAsync(request, cts.Token);
        }
        catch
        {
            // silently fail
        }
    }

    private static string BuildEnvironmentDetails()
    {
        return $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
               $"Application Name: {GetAppName()}\n" +
               $"Application Version: {GetVersion()}\n" +
               $"OS Version: {Environment.OSVersion}\n" +
               $"Architecture: {RuntimeInformation.OSArchitecture}\n" +
               $"Bitness: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}\n" +
               $"Windows Version: {RuntimeInformation.OSDescription}\n" +
               $"Processor Count: {Environment.ProcessorCount}\n" +
               $"Base Directory: {AppContext.BaseDirectory}\n" +
               $"Temp Path: {Path.GetTempPath()}";
    }

    private static string BuildExceptionDetails(Exception ex)
    {
        return $"Type: {ex.GetType().FullName}\n" +
               $"Message: {ex.Message}\n" +
               $"Source: {ex.Source}\n" +
               $"StackTrace: {ex.StackTrace}";
    }

    private static string GetAppName()
    {
        try { return Assembly.GetEntryAssembly()?.GetName().Name ?? "SimpleChdDrive"; }
        catch { return "SimpleChdDrive"; }
    }

    private static string GetVersion()
    {
        try { return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0"; }
        catch { return "1.0.0"; }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "";
        return value[..(maxLength - 3)] + "...";
    }
}
