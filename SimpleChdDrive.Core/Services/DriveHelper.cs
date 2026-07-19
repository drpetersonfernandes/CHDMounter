namespace SimpleChdDrive.Core.Services;

public static class DriveHelper
{
    public static string PickDriveLetter()
    {
        var drives = DriveInfo.GetDrives().Select(static d => d.Name[0]).ToHashSet();
        for (var c = 'M'; c <= 'Q'; c++)
            if (!drives.Contains(c))
                return $"{c}:";

        return "Z:";
    }
}
