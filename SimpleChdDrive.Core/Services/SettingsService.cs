namespace SimpleChdDrive.Core.Services;

public interface ISettingsService
{
    string DefaultMountPoint { get; set; }
}

public class SettingsService : ISettingsService
{
    public string DefaultMountPoint { get; set; } = "";
}
