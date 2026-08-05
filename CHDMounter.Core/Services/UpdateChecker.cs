using System.Runtime.InteropServices;
using System.Text.Json;

namespace CHDMounter.Core.Services;

/// <summary>
/// Checks for application updates by querying the GitHub releases API.
/// Results are cached on disk so repeated launches do not hammer the API
/// (GitHub's unauthenticated rate limit is 60 requests/hour per IP).
/// </summary>
public static class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/drpetersonfernandes/CHDMounter/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/drpetersonfernandes/CHDMounter/releases/latest";
    private const string CacheFileName = "update-check.json";

    /// <summary>
    /// How long a successful check result is reused before querying the API again.
    /// </summary>
    private static readonly TimeSpan SuccessCacheLifetime = TimeSpan.FromHours(24);

    /// <summary>
    /// How long a failed check suppresses further API calls (prevents rate-limit hammering).
    /// </summary>
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromHours(1);

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

            var cached = ReadCache();
            if (cached is not null && IsCacheUsable(cached, DateTime.UtcNow))
            {
                if (cached.Succeeded)
                {
                    ApplyResult(cached, currentVersion);
                    return;
                }

                // Last attempt failed recently (e.g. network or rate limit).
                // Do not hammer the API; silently report no update.
                var age = DateTime.UtcNow - cached.CheckedAtUtc;
                Serilog.Log.Information("UpdateChecker: Skipping check (last attempt failed {AgeMinutes:N0} minutes ago).", age.TotalMinutes);
                Result = new UpdateCheckResult
                {
                    HasUpdate = false,
                    CurrentVersion = currentVersion
                };
                return;
            }

            var json = await Client.GetStringAsync(GitHubApiUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "0.0.0";
            var latestVersion = tagName.StartsWith("release_", StringComparison.Ordinal) ? tagName[8..] : tagName;
            var releaseUrl = root.GetProperty("html_url").GetString() ?? ReleasesPageUrl;

            var downloadUrl = releaseUrl;

            if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            {
                var variant = GetVariant();
                var arch = GetArchitecture();

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Contains(variant, StringComparison.OrdinalIgnoreCase)
                        && name.Contains(arch, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? releaseUrl;
                        break;
                    }
                }
            }

            var result = new UpdateCheckCache
            {
                CheckedAtUtc = DateTime.UtcNow,
                Succeeded = true,
                LatestVersion = latestVersion,
                ReleaseUrl = releaseUrl,
                DownloadUrl = downloadUrl
            };

            WriteCache(result);
            ApplyResult(result, currentVersion);
        }
        catch (Exception ex)
        {
            // A failed update check is transient/environmental (network outage, GitHub
            // rate limiting, release not published yet). It is not a bug in this
            // application, so log below Warning to keep it out of bug reports.
            Serilog.Log.Information(ex, "UpdateChecker: Failed to check for updates: {Message}", ex.Message);

            WriteCache(new UpdateCheckCache
            {
                CheckedAtUtc = DateTime.UtcNow,
                Succeeded = false
            });

            Result = new UpdateCheckResult
            {
                HasUpdate = false,
                CurrentVersion = AppInfoHelper.GetVersion()
            };
        }
    }

    /// <summary>
    /// Determines whether a cached check result is still usable (i.e. fresh enough to
    /// avoid another API call): successful results for <see cref="SuccessCacheLifetime"/>,
    /// failed results only for <see cref="FailureRetryDelay"/>.
    /// </summary>
    /// <param name="cache">The cached check result.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns><c>true</c> if the cache entry should be used without hitting the API.</returns>
    internal static bool IsCacheUsable(UpdateCheckCache cache, DateTime utcNow)
    {
        var age = utcNow - cache.CheckedAtUtc;
        return cache.Succeeded ? age < SuccessCacheLifetime : age < FailureRetryDelay;
    }

    private static void ApplyResult(UpdateCheckCache cache, string currentVersion)
    {
        var hasUpdate = IsNewer(cache.LatestVersion, currentVersion);

        Result = new UpdateCheckResult
        {
            HasUpdate = hasUpdate,
            CurrentVersion = currentVersion,
            LatestVersion = hasUpdate ? cache.LatestVersion : currentVersion,
            ReleaseUrl = cache.ReleaseUrl,
            DownloadUrl = cache.DownloadUrl
        };
    }

    private static string? _cachePathOverride;

    /// <summary>
    /// Overrides the cache file location. Intended for tests so they never touch the
    /// real application data folder.
    /// </summary>
    /// <param name="path">The cache file path to use, or <c>null</c> to restore the default.</param>
    internal static void SetCachePath(string? path)
    {
        _cachePathOverride = path;
    }

    internal static string GetCachePath()
    {
        if (_cachePathOverride is not null)
            return _cachePathOverride;

        var folder = DiagnosticLogger.GetAppDataFolder(AppInfoHelper.GetAppName());
        return Path.Combine(folder, CacheFileName);
    }

    internal static UpdateCheckCache? ReadCache()
    {
        try
        {
            var path = GetCachePath();
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UpdateCheckCache>(json);
        }
        catch
        {
            return null;
        }
    }

    internal static void WriteCache(UpdateCheckCache cache)
    {
        try
        {
            var path = GetCachePath();
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(cache);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Caching is best-effort; never let it break the update check.
        }
    }

    private static string GetVariant()
    {
        var appName = AppInfoHelper.GetAppName();
        return appName.Contains("WinFsp", StringComparison.OrdinalIgnoreCase) ? "WinFsp" : "Dokan";
    }

    private static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };
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

    internal sealed class UpdateCheckCache
    {
        public DateTime CheckedAtUtc { get; set; }

        public bool Succeeded { get; set; }

        public string LatestVersion { get; set; } = "0.0.0";

        public string ReleaseUrl { get; set; } = ReleasesPageUrl;

        public string DownloadUrl { get; set; } = ReleasesPageUrl;
    }
}
