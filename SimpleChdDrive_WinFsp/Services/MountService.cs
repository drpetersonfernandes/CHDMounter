using System.Security.AccessControl;
using System.Security.Principal;
using Fsp;
using Microsoft.Win32;
using SimpleChdDrive.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;

#pragma warning disable CA1707
namespace SimpleChdDrive_WinFsp.Services;
#pragma warning restore CA1707

internal class MountService : IMountService, IDisposable
{
    private readonly ILoggingService _loggingService;
    private FileSystemHost? _host;
    private ChdFs? _currentFs;
    private ChdContainer? _container;

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
            _container = null;
            return;
        }

        _loggingService.Log($"Parsing complete. Volume: {_container.VolumeName}");

        var crossIntegrity = IsRunningAsAdministrator();
        if (crossIntegrity)
            _loggingService.Log("Running as Administrator: Cross-integrity mount enforced so standard processes can access the drive.");

        if (string.IsNullOrEmpty(mountPoint))
        {
            MountPoint = crossIntegrity
                ? GetCrossIntegrityMountPath(chdPath)
                : DriveHelper.PickDriveLetter();
        }
        else
        {
            if (crossIntegrity && IsDriveLetterMountPoint(mountPoint))
            {
                _loggingService.Log("Cross-integrity mode: Drive letter mounts are not supported. Redirecting to folder mount.");
                MountPoint = GetCrossIntegrityMountPath(chdPath);
            }
            else
            {
                MountPoint = mountPoint;
            }
        }

        _loggingService.Log($"Mounting at {MountPoint} (WinFsp)...");

        var isDriveLetter = IsDriveLetterMountPoint(MountPoint);
        var persistentAcls = crossIntegrity && !isDriveLetter;

        _currentFs = new ChdFs(_container, _loggingService, persistentAcls);
        _host = new FileSystemHost(_currentFs);

        var securityDescriptor = persistentAcls ? CreateCrossIntegritySecurityDescriptor() : null;
        if (securityDescriptor != null)
            _loggingService.Log("Cross-integrity: using permissive DACL (Everyone Full Access).");

        _host.Mount(MountPoint, securityDescriptor, true, unchecked((uint)-1));

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
        _host = null;
        _currentFs?.Dispose();
        _currentFs = null;
        _container = null;
        IsMounted = false;
        MountPoint = "";
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static byte[] CreateCrossIntegritySecurityDescriptor()
    {
        const string sddl = "D:P(A;;FA;;;WD)";
        var sd = new RawSecurityDescriptor(sddl);
        var bytes = new byte[sd.BinaryLength];
        sd.GetBinaryForm(bytes, 0);
        return bytes;
    }

    private static string GetCrossIntegrityMountPath(string chdPath)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SimpleChdDrive", "Mounts");
        var folderName = Path.GetFileNameWithoutExtension(chdPath);
        return Path.Combine(baseDir, SanitizeFolderName(folderName));
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (invalid.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }
        return new string(chars);
    }

    private static bool IsDriveLetterMountPoint(string mountPoint)
    {
        return mountPoint is [_, ':', ..] && char.IsLetter(mountPoint[0])
                                          && (mountPoint.Length == 2 || mountPoint is [_, _, '\\']);
    }

    public void Dispose()
    {
        Unmount();
        GC.SuppressFinalize(this);
    }
}
