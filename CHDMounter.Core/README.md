# CHDMounter.Core

Shared core library for the CHDMounter solution. Provides common services, logging infrastructure, WPF views, settings, and interfaces used by both the Dokan and WinFsp frontend applications.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Components

### Interfaces

| Interface | Description |
|-----------|-------------|
| `ILoggingService` | Contract for application logging with UI binding |
| `IMountService` | Contract for virtual drive mount/unmount operations |
| `ISettingsService` | Contract for settings persistence |
| `IScreenshotService` | Contract for screenshot capture |

### Services

| Service | Description |
|---------|-------------|
| `LoggingService` | WPF-dispatched `ObservableCollection<LogEntry>` with max 5000 entries, dedup within 100ms |
| `ServiceProvider` | Simple DI container using `ConcurrentDictionary<Type, object>` |
| `SettingsService` | DPAPI-encrypted JSON settings persistence at `%LocalAppData%` |
| `ConsoleTypeHelper` | Maps CLI arguments (numeric or string) to `ConsoleType` enum values |
| `DriveHelper` | Auto-selects available drive letter (M-Q preferred, Z-D fallback) |
| `UpdateChecker` | Checks GitHub releases API for new versions |
| `BugReportClient` | Rate-limited queue sending error/warning reports to remote API |
| `StatsClient` | One-shot anonymous usage telemetry |
| `ScreenshotService` | Win32 foreground window capture → PNG |

### Logging

| Component | Description |
|-----------|-------------|
| `AppLogger` | Serilog configuration: File (rolling daily, 7 retained) + Debug + BugReportSink |
| `DiagnosticLogger` | Temp-file debug logger with automatic cleanup (>7 days) |
| `ErrorLogger` | Global `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` handlers |
| `BugReportSink` | Serilog `ILogEventSink` that forwards Warning+ events to BugReportClient |
| `LogTextWriter` | `TextWriter` wrapper redirecting `Console.WriteLine` to `ILoggingService` |

### Views

| View | Description |
|------|-------------|
| `MainWindowBase` | Shared base class for Dokan/WinFsp MainWindow UI logic (console dropdown, mount/unmount, update banner, F8 screenshot) |
| `ConsoleSelectionWindow` | GUI dialog for selecting console file system type |
| `SettingsWindow` | Settings dialog (auto-open mounted drive) |
| `AboutWindow` | Application about dialog |

### Models

| Model | Description |
|-------|-------------|
| `LogEntry` | Timestamped log message with error flag |
| `AppSettings` | Application settings (`AutoOpenMountedDrive`) |
| `UpdateCheckResult` | Version check result from GitHub API |

### Themes

| Resource | Description |
|----------|-------------|
| `DarkTheme.xaml` | WPF-UI dark theme `ResourceDictionary` |

## Dependencies

- **Serilog** 4.4.0 — Structured logging
- **Serilog.Sinks.File** 7.0.0 — Rolling file logs
- **Serilog.Sinks.Debug** 3.0.0 — VS debug output
- **WPF-UI** 4.3.0 — Modern WPF theming
- **VideoGameFileSystemParser** — CHD file system parsing library (project reference)

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/CHDMounter/blob/main/LICENSE) file for details.
