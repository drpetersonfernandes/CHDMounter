namespace SimpleChdDrive.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
}
