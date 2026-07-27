using System.Text.Json;

namespace CHDMounter.Core.Services;

/// <summary>
/// Checks for application updates by querying the GitHub releases API.
/// </summary>
public static class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/drpetersonfernandes/CHDMounter/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/drpetersonfernandes/CHDMounter/releases/latest";
    private static readonly HttpClient Client = new();
    private static int _started;

    private static UpdateCheckResult? _result;

    /// <summary>
    /// Gets the result of the last update check, or <c>null</c> if no check has completed.
    /// </summary>
    public static UpdateCheckResult? Result
    {
        get => Volatile.Read(ref _result);
        private set => Volatile.Write(ref _result, value);
    }

    static UpdateChecker()
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("CHDMounter");
        Client.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Initiates an asynchronous update check. Only the first call per process lifetime takes effect.
    /// </summary>
    public static void CheckForUpdates()
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            return;

        _ = Task.Run(CheckAsync);
    }

    private static async Task CheckAsync()
    {
        try
        {
            var currentVersion = AppInfoHelper.GetVersion();
            var json = await Client.GetStringAsync(GitHubApiUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "0.0.0";
            var latestVersion = tagName.StartsWith('v') ? tagName[1..] : tagName;
            var releaseUrl = root.GetProperty("html_url").GetString() ?? ReleasesPageUrl;

            var downloadUrl = releaseUrl;

            if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            {
                var appName = AppInfoHelper.GetAppName().ToLowerInvariant();

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString()?.ToLowerInvariant() ?? "";
                    if (name.Contains(appName, StringComparison.Ordinal))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? releaseUrl;
                        break;
                    }
                }
            }

            var hasUpdate = IsNewer(latestVersion, currentVersion);

            Result = new UpdateCheckResult
            {
                HasUpdate = hasUpdate,
                CurrentVersion = currentVersion,
                LatestVersion = hasUpdate ? latestVersion : currentVersion,
                ReleaseUrl = releaseUrl,
                DownloadUrl = downloadUrl
            };
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "UpdateChecker: Failed to check for updates");
            Result = new UpdateCheckResult
            {
                HasUpdate = false,
                CurrentVersion = AppInfoHelper.GetVersion()
            };
        }
    }

    private static bool IsNewer(string latest, string current)
    {
        try
        {
            var v1 = new Version(latest);
            var v2 = new Version(current);
            return v1 > v2;
        }
        catch
        {
            return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
        }
    }
}
