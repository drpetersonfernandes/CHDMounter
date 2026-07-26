namespace CHDMounter.Core.Services;

/// <summary>
/// Provides helper methods for selecting available drive letters.
/// </summary>
public static class DriveHelper
{
    /// <summary>
    /// Picks an available drive letter from the range M-Q, falling back to Z.
    /// </summary>
    /// <returns>A drive letter string in the format "X:" (e.g., "M:").</returns>
    public static string PickDriveLetter()
    {
        var drives = DriveInfo.GetDrives().Select(static d => d.Name[0]).ToHashSet();
        for (var c = 'M'; c <= 'Q'; c++)
            if (!drives.Contains(c))
                return $"{c}:";

        for (var c = 'Z'; c >= 'D'; c--)
            if (!drives.Contains(c))
                return $"{c}:";

        return "C:";
    }
}
