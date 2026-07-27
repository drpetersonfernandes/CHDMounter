using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using Fsp;
using Microsoft.Win32;
using CHDMounter.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;

#pragma warning disable CA1707
namespace CHDMounter_WinFsp.Services;

/// <summary>
/// Mounts and unmounts CHD disc images as virtual drives using the WinFsp file system driver.
/// Supports cross-integrity mounts when running as Administrator.
/// </summary>
internal class MountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly Lock _mountLock = new();
    private FileSystemHost? _host;
    private ChdFs? _currentFs;
    private ChdContainer? _container;

    /// <inheritdoc/>
    public bool IsMounted { get; private set; }

    /// <inheritdoc/>
    public string MountPoint { get; private set; } = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="MountService"/> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    internal MountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public bool CanMount()
    {
        return IsWinFspInstalled();
    }

    /// <inheritdoc/>
    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        lock (_mountLock)
        {
            if (IsMounted) throw new InvalidOperationException("Already mounted.");

            if (!IsWinFspInstalled())
            {
                _loggingService.LogError("WinFsp not found. Unable to mount CHD.");
                ShowWinFspNotInstalledDialog();
                return;
            }

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

            _currentFs = new ChdFs(_container, persistentAcls);
            _host = new FileSystemHost(_currentFs);

            var securityDescriptor = persistentAcls ? CreateCrossIntegritySecurityDescriptor() : null;
            if (securityDescriptor is not null)
                _loggingService.Log("Cross-integrity: using permissive DACL (Everyone Full Access).");

            _host.Mount(MountPoint, securityDescriptor, true, unchecked((uint)-1));

            IsMounted = true;
            _loggingService.Log($"Mounted at {MountPoint} (WinFsp).");
        }
    }

    /// <inheritdoc/>
    public void Unmount()
    {
        lock (_mountLock)
        {
            if (!IsMounted) return;

            _loggingService.Log($"Unmounting {MountPoint} (WinFsp)...");
            if (_host is not null)
            {
                try
                {
                    _host.Unmount();
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error: {ex.Message}");
                }
            }

            _host?.Dispose();
            _host = null;
            _currentFs?.Dispose();
            _currentFs = null;
            _container?.Dispose();
            _container = null;
            IsMounted = false;
            MountPoint = "";
        }
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
            "CHDMounter", "Mounts");
        var folderName = Path.GetFileNameWithoutExtension(chdPath);
        var mountPath = Path.Combine(baseDir, SanitizeFolderName(folderName));
        Directory.CreateDirectory(mountPath);
        return mountPath;
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

    /// <inheritdoc/>
    public void Dispose()
    {
        Unmount();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static bool IsWinFspInstalled()
    {
        return EnsureWinFspOnPath();
    }

    private static bool EnsureWinFspOnPath()
    {
        try
        {
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (currentPath.Contains("WinFsp", StringComparison.OrdinalIgnoreCase))
                return true;

            var binDir = FindWinFspBinDir();
            if (binDir is null)
                return false;

            var dllName = Environment.Is64BitProcess ? "winfsp-x64.dll" : "winfsp-x86.dll";
            var dllPath = Path.Combine(binDir, dllName);
            if (!File.Exists(dllPath))
                return false;

            Environment.SetEnvironmentVariable("PATH", binDir + ";" + currentPath, EnvironmentVariableTarget.Process);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindWinFspBinDir()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp")
                            ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp");
            if (key is null)
                return null;

            var sxsDir = key.GetValue("SxsDir") as string;
            if (!string.IsNullOrEmpty(sxsDir))
            {
                var sxsBin = Path.Combine(sxsDir, "bin");
                if (Directory.Exists(sxsBin))
                    return sxsBin;
            }

            var installDir = key.GetValue("InstallDir") as string;
            if (!string.IsNullOrEmpty(installDir))
            {
                var installBin = Path.Combine(installDir, "bin");
                if (Directory.Exists(installBin))
                    return installBin;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to find WinFsp binary directory");
        }

        return null;
    }

    private static void ShowWinFspNotInstalledDialog()
    {
        const string message = "The WinFsp file system driver is required to mount CHD files as virtual drives. " +
                               "It does not appear to be installed on this system.\n\n" +
                               "Would you like to open the WinFsp download page?";

        var result = MessageBox.Show(message, "WinFsp Not Found",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/winfsp/winfsp/releases",
                UseShellExecute = true
            });
        }
    }
}
