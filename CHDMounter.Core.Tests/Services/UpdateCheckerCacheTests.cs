namespace CHDMounter.Core.Tests.Services;

[CollectionDefinition("UpdateCheckerCache", DisableParallelization = true)]
public class UpdateCheckerCacheCollection;

[Collection("UpdateCheckerCache")]
public class UpdateCheckerCacheTests : IDisposable
{
    private readonly string _tempFolder;

    public UpdateCheckerCacheTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "CHDMounter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
        UpdateChecker.SetCachePath(Path.Combine(_tempFolder, "update-check.json"));
    }

    public void Dispose()
    {
        UpdateChecker.SetCachePath(null);
        try
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static UpdateChecker.UpdateCheckCache CreateCache(DateTime checkedAtUtc, bool succeeded, string? latestVersion = null)
    {
        return new UpdateChecker.UpdateCheckCache
        {
            CheckedAtUtc = checkedAtUtc,
            Succeeded = succeeded,
            LatestVersion = latestVersion ?? "0.0.0"
        };
    }

    [Fact]
    public void WriteCacheThenReadCacheReturnsSameData()
    {
        var cache = new UpdateChecker.UpdateCheckCache
        {
            CheckedAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
            Succeeded = true,
            LatestVersion = "1.2.3",
            ReleaseUrl = "https://github.com/drpetersonfernandes/CHDMounter/releases/latest",
            DownloadUrl = "https://github.com/drpetersonfernandes/CHDMounter/releases/download/release_1.2.3/CHDMounter_x64.zip"
        };

        UpdateChecker.WriteCache(cache);
        var read = UpdateChecker.ReadCache();

        Assert.NotNull(read);
        Assert.Equal(cache.CheckedAtUtc, read.CheckedAtUtc);
        Assert.True(read.Succeeded);
        Assert.Equal("1.2.3", read.LatestVersion);
        Assert.Equal(cache.ReleaseUrl, read.ReleaseUrl);
        Assert.Equal(cache.DownloadUrl, read.DownloadUrl);
    }

    [Fact]
    public void ReadCacheReturnsNullWhenFileMissing()
    {
        Assert.Null(UpdateChecker.ReadCache());
    }

    [Fact]
    public void ReadCacheReturnsNullForCorruptContent()
    {
        UpdateChecker.WriteCache(CreateCache(DateTime.UtcNow, true));
        File.WriteAllText(UpdateChecker.GetCachePath(), "{ not valid json !!!");

        Assert.Null(UpdateChecker.ReadCache());
    }

    [Fact]
    public void WriteFailedCacheThenReadReportsFailure()
    {
        UpdateChecker.WriteCache(CreateCache(DateTime.UtcNow, false));

        var read = UpdateChecker.ReadCache();

        Assert.NotNull(read);
        Assert.False(read.Succeeded);
    }

    [Fact]
    public void SuccessfulCacheIsUsableWithinLifetime()
    {
        var cache = CreateCache(DateTime.UtcNow.AddHours(-23), true);

        Assert.True(UpdateChecker.IsCacheUsable(cache, DateTime.UtcNow));
    }

    [Fact]
    public void SuccessfulCacheExpiresAfterLifetime()
    {
        var cache = CreateCache(DateTime.UtcNow.AddHours(-25), true);

        Assert.False(UpdateChecker.IsCacheUsable(cache, DateTime.UtcNow));
    }

    [Fact]
    public void FailedCacheIsUsableOnlyWithinRetryDelay()
    {
        var fresh = CreateCache(DateTime.UtcNow.AddMinutes(-30), false);
        var stale = CreateCache(DateTime.UtcNow.AddHours(-2), false);

        Assert.True(UpdateChecker.IsCacheUsable(fresh, DateTime.UtcNow));
        Assert.False(UpdateChecker.IsCacheUsable(stale, DateTime.UtcNow));
    }

    [Fact]
    public void CacheIsUsableWithFutureTimestamp()
    {
        // Clock skew: a cache stamped in the future must still be considered fresh.
        var cache = CreateCache(DateTime.UtcNow.AddHours(1), true);

        Assert.True(UpdateChecker.IsCacheUsable(cache, DateTime.UtcNow));
    }
}
