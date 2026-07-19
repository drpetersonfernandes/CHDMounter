# SimpleChdDrive.Core

Shared core library for the SimpleChdDrive solution. Provides common services, logging infrastructure, WPF views, and interfaces used by both the Dokan and WinFsp frontend applications.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Components

### Services

| Service | Description |
|---------|-------------|
| `LoggingService` | WPF-dispatched log collection via `ObservableCollection<LogEntry>` |
| `ServiceProvider` | Simple dependency injection container |
| `StatsClient` | Anonymous usage statistics (fire-and-forget) |
| `BugReportClient` | Error/warning crash reporting queue |

### Interfaces

| Interface | Description |
|-----------|-------------|
| `ILoggingService` | Contract for application logging with UI binding |
| `IMountService` | Contract for virtual drive mount/unmount operations |

### Logging

| Component | Description |
|-----------|-------------|
| `DiagnosticLogger` | Temp-file debug logger with automatic cleanup (>7 days) |
| `AppLogger` | Serilog configuration and lifecycle management |
| `ErrorLoggerStatic` | Global unhandled exception handlers |
| `BugReportSink` | Serilog sink that forwards warnings/errors to BugReportClient |
| `LogTextWriter` | Console output redirection to ILoggingService |

### Views

| View | Description |
|------|-------------|
| `ConsoleSelectionWindow` | GUI dialog for selecting console file system type |
| `AboutWindow` | Application about dialog |

### Models

| Model | Description |
|-------|-------------|
| `LogEntry` | Timestamped log message with error flag |

## Dependencies

- **Serilog** 4.4.0 — Structured logging
- **VideoGameFileSystemParser** — CHD file system parsing library
- **WPF-UI** (transitive)

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE) file for details.
