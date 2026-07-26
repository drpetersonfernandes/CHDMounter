using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CHDMounter.Core.Services;

/// <summary>
/// Sends anonymous application usage statistics to a remote analytics endpoint.
/// </summary>
public static class StatsClient
{
    private const string BaseUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string ApiKeyEncoded = "YUdwb04zbDFOblExTm5SNWNqVTBNRzg1ZFRnM05qYzJOelp5TlRZM05EVXpORFExTXpJek5USTJOR00zTldJMmREZG5aMmRvWjJjM05uUnlaalUyTkdVPQ==";
    private static readonly HttpClient Client = new();
    private static int _sent;

    /// <summary>
    /// Sends application statistics once per process lifetime. Subsequent calls are ignored.
    /// </summary>
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
            request.Headers.Add("Authorization", $"Bearer {GetApiKey()}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Client.SendAsync(request, cts.Token);
        }
        catch
        {
            // silently fail
        }
    }

    private static string GetApiKey()
    {
        var once = Encoding.UTF8.GetString(Convert.FromBase64String(ApiKeyEncoded));
        return Encoding.UTF8.GetString(Convert.FromBase64String(once));
    }

    private static string GetAppId()
    {
        try
        {
            return (Assembly.GetEntryAssembly()?.GetName().Name ?? "CHDMounter").ToLowerInvariant();
        }
        catch
        {
            return "CHDMounter";
        }
    }

    private static string GetVersion()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
}
