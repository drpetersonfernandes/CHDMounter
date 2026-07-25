namespace SimpleChdDrive.Core.Models;

/// <summary>
/// Represents the result of an application update check against the GitHub releases API.
/// </summary>
public class UpdateCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether a newer version is available.
    /// </summary>
    public bool HasUpdate { get; set; }

    /// <summary>
    /// Gets or sets the current application version string.
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latest available version string.
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the GitHub release page.
    /// </summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direct download URL for the latest release asset.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;
}
