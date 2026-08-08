using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using DokanNet;
using DokanNet.Logging;
using CHDMounter.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Services;

/// <summary>
/// Mounts and unmounts CHD disc images as virtual drives using the Dokan file system driver.
/// </summary>
internal class MountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly Lock _mountLock = new();
    private DokanInstance? _dokanInstance;
    private ChdFs? _currentFs;
    private ChdContainer? _container;

    /// <inheritdoc/>
    public bool IsMounted { get; private set; }

    /// <inheritdoc/>
    public string MountPoint { get; private set; } = "";

    [DllImport("dokan2.dll", ExactSpelling = true)]
    private static extern uint DokanVersion();

    /// <summary>
    /// Initializes a new instance of the <see cref="MountService"/> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    public MountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public bool CanMount()
    {
        return IsDokanInstalled();
    }

    /// <inheritdoc/>
    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        lock (_mountLock)
        {
            if (IsMounted)
                throw new InvalidOperationException("Already mounted.");

            if (!IsDokanInstalled())
            {
                _loggingService.LogError("Dokan driver not found. Unable to mount CHD.");
                ShowDokanNotInstalledDialog();
                return;
            }

            _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType}...");

            try
            {
                _container = new ChdContainer(chdPath);
                if (!_container.MountAndParse(consoleType))
                {
                    _loggingService.LogError($"Failed to open or parse CHD as {consoleType}.");
                    return;
                }

                _loggingService.Log($"Parsing complete. Volume: {_container.VolumeName}");

                // Per-session or elevated-session drives can be invisible to
                // DriveInfo.GetDrives(), so the picked letter may already be in use.
                // Enumerate candidates and retry when Dokan cannot mount at one.
                // An explicitly requested drive letter is tried first, then falls
                // back to auto-assigned available letters if it is unavailable.
                // Folder-path mount points are used as-is (no fallback).
                var candidates = string.IsNullOrEmpty(mountPoint)
                    ? DriveHelper.GetAvailableDriveLetters().ToList()
                    : IsDriveLetterMountPoint(mountPoint)
                        ? [mountPoint, .. DriveHelper.GetAvailableDriveLetters()]
                        : [mountPoint];

                _currentFs = new ChdFs(_container, _loggingService);

                var dokan = new Dokan(new DokanPrefixedLogger(_loggingService));
                Exception? lastError = null;

                foreach (var candidate in candidates)
                {
                    MountPoint = candidate;
                    _loggingService.Log($"Mounting at {MountPoint}...");

                    try
                    {
                        var builder = new DokanInstanceBuilder(dokan)
                            .ConfigureOptions(options =>
                            {
                                options.Options = DokanOptions.RemovableDrive;
                                options.MountPoint = MountPoint;
                            });

                        _dokanInstance = builder.Build(_currentFs);
                        IsMounted = true;
                        _loggingService.Log($"Mounted at {MountPoint}. {_dokanInstance}");
                        return;
                    }
                    catch (Exception ex) when (candidates.Count > 1)
                    {
                        // The drive letter may be used by another volume (per-session
                        // drives can be invisible to DriveInfo.GetDrives()). Try the next one.
                        lastError = ex;
                        _loggingService.Log($"Mount point {MountPoint} is unavailable ({ex.Message}). Trying next...");
                    }
                }

                throw lastError ?? new InvalidOperationException("No available drive letter found. All drive letters are in use.");
            }
            finally
            {
                if (!IsMounted)
                {
                    _dokanInstance?.Dispose();
                    _dokanInstance = null;
                    _currentFs?.Dispose();
                    _currentFs = null;
                    _container?.Dispose();
                    _container = null;
                    MountPoint = "";
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Unmount()
    {
        lock (_mountLock)
        {
            if (!IsMounted) return;

            _loggingService.Log($"Unmounting {MountPoint}...");
            if (_dokanInstance is not null)
            {
                try
                {
                    _dokanInstance.Dispose();
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error during unmount: {ex.Message}");
                }
            }

            _dokanInstance = null;
            _currentFs?.Dispose();
            _currentFs = null;
            _container?.Dispose();
            _container = null;
            IsMounted = false;
            MountPoint = "";
        }
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

    private static bool IsDokanInstalled()
    {
        try
        {
            return DokanVersion() > 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool IsDriveLetterMountPoint(string mountPoint)
    {
        return mountPoint is [_, ':', ..] && char.IsLetter(mountPoint[0])
                                          && (mountPoint.Length == 2 || mountPoint is [_, _, '\\']);
    }

    private static void ShowDokanNotInstalledDialog()
    {
        const string message = "The Dokan file system driver (dokan2.dll) is required to mount CHD files as virtual drives. " +
                               "It does not appear to be installed on this system.\n\n" +
                               "Would you like to open the Dokan download page?";

        var result = MessageBox.Show(message, "Dokan Driver Not Found",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/dokan-dev/dokany/releases",
                UseShellExecute = true
            });
        }
    }
}

/// <summary>
/// An adapter that routes Dokan log messages to the application's <see cref="ILoggingService"/>.
/// </summary>
internal class DokanPrefixedLogger : ILogger
{
    private readonly ILoggingService _loggingService;

    /// <inheritdoc/>
    public bool DebugEnabled => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="DokanPrefixedLogger"/> class.
    /// </summary>
    /// <param name="loggingService">The logging service to write messages to.</param>
    internal DokanPrefixedLogger(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void Debug(string message, params object[] args)
    {
        _loggingService.Log($"[Dokan:DBG] {string.Format(CultureInfo.InvariantCulture, message, args)}");
    }

    public void Info(string message, params object[] args)
    {
        _loggingService.Log($"[Dokan:INF] {string.Format(CultureInfo.InvariantCulture, message, args)}");
    }

    public void Warn(string message, params object[] args)
    {
        _loggingService.Log($"[Dokan:WRN] {string.Format(CultureInfo.InvariantCulture, message, args)}");
    }

    public void Error(string message, params object[] args)
    {
        _loggingService.LogError($"[Dokan] {string.Format(CultureInfo.InvariantCulture, message, args)}");
    }

    public void Fatal(string message, params object[] args)
    {
        _loggingService.LogError($"[Dokan] {string.Format(CultureInfo.InvariantCulture, message, args)}");
    }
}
