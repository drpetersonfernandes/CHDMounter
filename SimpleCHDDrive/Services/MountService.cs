using System.Globalization;
using System.Runtime.InteropServices;
using DokanNet;
using DokanNet.Logging;
using SimpleChdDrive.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;

namespace SimpleChdDrive.Services;

internal class MountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private DokanInstance? _dokanInstance;
    private ChdFs? _currentFs;
    private ChdContainer? _container;

    public bool IsMounted { get; private set; }
    public string MountPoint { get; private set; } = "";

    [DllImport("dokan2.dll", ExactSpelling = true)]
    private static extern uint DokanVersion();

    public MountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public bool CanMount()
    {
        try
        {
            var version = DokanVersion();
            _loggingService.Log($"Dokan version: {version}");
            return version > 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Dokan driver not found: {ex.Message}");
            return false;
        }
    }

    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        if (IsMounted)
            throw new InvalidOperationException("Already mounted.");

        _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType}...");

        _container = new ChdContainer(chdPath);
        if (!_container.MountAndParse(consoleType))
        {
            _loggingService.LogError($"Failed to open or parse CHD as {consoleType}.");
            _container.Dispose();
            _container = null;
            return;
        }

        _loggingService.Log($"Parsing complete. Volume: {_container.VolumeName}");

        MountPoint = mountPoint ?? DriveHelper.PickDriveLetter();
        _loggingService.Log($"Mounting at {MountPoint}...");

        _currentFs = new ChdFs(_container, _loggingService);

        var dokan = new Dokan(new DokanPrefixedLogger(_loggingService));
        var builder = new DokanInstanceBuilder(dokan)
            .ConfigureOptions(options =>
            {
                options.Options = DokanOptions.RemovableDrive;
                options.MountPoint = MountPoint;
            });

        _dokanInstance = builder.Build(_currentFs);
        IsMounted = true;
        _loggingService.Log($"Mounted at {MountPoint}. {_dokanInstance}");
    }

    public void Unmount()
    {
        if (!IsMounted) return;

        _loggingService.Log($"Unmounting {MountPoint}...");
        if (_dokanInstance != null)
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

    public void Dispose()
    {
        Unmount();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

internal class DokanPrefixedLogger : ILogger
{
    private readonly ILoggingService _loggingService;
    public bool DebugEnabled => false;

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
