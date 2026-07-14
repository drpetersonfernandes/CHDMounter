using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SimpleChdDrive.Core.Services;

public static class StatsClient
{
    private const string BaseUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string ApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private static readonly HttpClient Client = new();
    private static int _sent;

    public static void SendStats()
    {
        if (Interlocked.CompareExchange(ref _sent, 1, 0) != 0)
            return;

        _ = Task.Run(SendAsync);
    }

    private static async Task SendAsync()
    {
        try
        {
            var payload = new
            {
                applicationId = GetAppId(),
                version = GetVersion()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Client.SendAsync(request, cts.Token);
        }
        catch
        {
            // silently fail
        }
    }

    private static string GetAppId()
    {
        try { return (Assembly.GetEntryAssembly()?.GetName().Name ?? "SimpleChdDrive").ToLowerInvariant(); }
        catch { return "SimpleChdDrive"; }
    }

    private static string GetVersion()
    {
        try { return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0"; }
        catch { return "1.0.0"; }
    }
}
