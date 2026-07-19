using Fsp;
using Microsoft.Win32;
using SimpleChdDrive.Core.Interfaces;
using SimpleChdDrive.Parsing.Parsers;

#pragma warning disable CA1707
namespace SimpleChdDrive_WinFsp.Services;
#pragma warning restore CA1707

public class MountService : IMountService, IDisposable
{
    private readonly ILoggingService _loggingService;
    private FileSystemHost _host = null!;
    private ChdFs _currentFs = null!;
    private ChdContainer _container = null!;

    public bool IsMounted { get; private set; }
    public string MountPoint { get; private set; } = "";

    public MountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public bool CanMount()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp")
                            ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp");
            if (key == null)
            { _loggingService.LogError("WinFsp not found.");
                return false; }

            _loggingService.Log("WinFsp detected.");
            return true;
        }
        catch (Exception ex)
        { _loggingService.LogError($"WinFsp detection failed: {ex.Message}");
            return false; }
    }

    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        if (IsMounted) throw new InvalidOperationException("Already mounted.");

        _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType} (WinFsp)...");

        _container = new ChdContainer(chdPath);
        if (!_container.MountAndParse(consoleType))
        {
            _loggingService.LogError($"Failed to open or parse CHD as {consoleType}.");
            _container.Dispose();
            _container = null!;
            return;
        }

        _loggingService.Log($"Parsing complete. Volume: {_container.VolumeName}");

        MountPoint = mountPoint ?? PickDriveLetter();
        _loggingService.Log($"Mounting at {MountPoint} (WinFsp)...");

        _currentFs = new ChdFs(_container, _loggingService);
        _host = new FileSystemHost(_currentFs);
        _host.Mount(MountPoint, null, true);

        IsMounted = true;
        _loggingService.Log($"Mounted at {MountPoint} (WinFsp).");
    }

    public void Unmount()
    {
        if (!IsMounted) return;

        _loggingService.Log($"Unmounting {MountPoint} (WinFsp)...");
        if (_host != null)
        {
            try { _host.Unmount(); }
            catch (Exception ex) { _loggingService.LogError($"Error: {ex.Message}"); }
        }

        _host?.Dispose();
        _host = null!;
        _currentFs?.Dispose();
        _currentFs = null!;
        _container?.Dispose();
        _container = null!;
        IsMounted = false;
        MountPoint = "";
    }

    private static string PickDriveLetter()
    {
        var drives = DriveInfo.GetDrives().Select(static d => d.Name[0]).ToHashSet();
        for (var c = 'M'; c <= 'Q'; c++)
            if (!drives.Contains(c))
                return $"{c}:";

        return "Z:";
    }

    public void Dispose()
    {
        Unmount();
        GC.SuppressFinalize(this);
    }
}
