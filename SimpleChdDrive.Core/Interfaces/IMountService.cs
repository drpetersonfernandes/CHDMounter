namespace SimpleChdDrive.Core.Interfaces;

public interface IMountService
{
    bool CanMount();
    void Mount(string chdPath, string? mountPoint, ConsoleType consoleType);
    void Unmount();
    bool IsMounted { get; }
    string MountPoint { get; }
}
