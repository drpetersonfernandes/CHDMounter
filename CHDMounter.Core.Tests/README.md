# SimpleChdDrive.Core.Tests

Integration and unit test project for the SimpleChdDrive solution. Validates file system parsers against real CHD disc images across 20+ console types, and tests all services, logging infrastructure, and models.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![xUnit](https://img.shields.io/badge/xUnit-2.9.3-green)](https://xunit.net/)

## Test Structure

### Parser Integration Tests (`Parsers/`)

Each parser has dedicated integration tests that open a real `.chd` file, parse its file system, and verify the resulting file/directory tree:

| Test | Console | Parser |
|------|---------|--------|
| `Ps1IntegrationTests` | PlayStation 1 | ISO 9660 |
| `Ps2IntegrationTests` | PlayStation 2 | ISO 9660 |
| `Ps3IntegrationTests` | PlayStation 3 | UDF |
| `PspIntegrationTests` | PSP | ISO 9660 (UMD) |
| `XboxIntegrationTests` | Xbox | XDVDFS |
| `DreamcastIntegrationTests` | Dreamcast | ISO 9660 + IP.BIN |
| `ThreeDoIntegrationTests` | 3DO | OperaFS |
| `CDiIntegrationTests` / `CDiDiagnosticTests` | CD-i | CD-i FS |
| `AmigaCd32IntegrationTests` | Amiga CD32 | ISO 9660 |
| `AmigaCdIntegrationTests` | Amiga CD | ISO 9660 |
| `PceCdIntegrationTests` | PC Engine CD | ISO 9660 |
| `PcFxIntegrationTests` / `PcFxDiagnosticTests` | PC-FX | PC-FX ISO |
| `FmTownsIntegrationTests` | FM Towns | ISO 9660 |
| `Pc98IntegrationTests` | PC-98 | ISO 9660 |
| `X68000IntegrationTests` | X68000 | ISO 9660 |
| `NeoGeoCdIntegrationTests` | Neo Geo CD | ISO 9660 |
| `SaturnIntegrationTests` | Saturn | ISO 9660 |
| `SegaGenesisCdIntegrationTests` | Sega Genesis CD | ISO 9660 |

### Parser Infrastructure Tests (`Parsers/`)

| Test | Description |
|------|-------------|
| `ChdContainerTests` | ChdContainer open/parse/read validation |
| `ParserFactoryTests` | ParserFactory ConsoleType → parser dispatch |
| `ParserFactoryExtendedTests` | Extended factory tests (all console types) |
| `SectorReaderTests` | SectorReader LBA mapping and sector reads |
| `SectorReaderHelperTests` | SectorReader helper method validation |

### Service Tests (`Services/`)

| Test | Description |
|------|-------------|
| `LoggingServiceTests` | WPF-dispatched log collection |
| `LoggingServiceExtendedTests` | Extended logging scenarios |
| `LoggingServiceThreadSafetyTests` | Concurrent logging thread safety |
| `ServiceProviderTests` | DI container registration and disposal |
| `ServiceProviderExtendedTests` | Extended DI scenarios |
| `ConsoleTypeHelperTests` | CLI argument → ConsoleType mapping |
| `DriveHelperTests` | Drive letter auto-selection |
| `SettingsServiceTests` | DPAPI settings persistence |
| `UpdateCheckerTests` | GitHub version check |
| `BugReportClientTests` | Error reporting queue |

### Logging Tests (`Logging/`)

| Test | Description |
|------|-------------|
| `AppLoggerTests` | Serilog configuration and lifecycle |
| `BugReportSinkTests` | Serilog sink → BugReportClient forwarding |
| `DiagnosticLoggerTests` | Temp-file logger with cleanup |
| `ErrorLoggerTests` | Global exception handler registration |
| `LogTextWriterTests` | Console → ILoggingService redirect |

### Model Tests (`Models/`)

| Test | Description |
|------|-------------|
| `EnumTests` | ConsoleType enumeration validation |
| `EnumExtendedTests` | Extended enum scenarios |
| `ModelTests` | Model behavior and serialization |
| `ModelExtendedTests` | Extended model scenarios |
| `AppSettingsTests` | AppSettings serialization |
| `UpdateCheckResultTests` | UpdateCheckResult model |

### Test Infrastructure

| Class | Description |
|-------|-------------|
| `ChdPathCatalog` | Centralized catalog of test CHD file paths, organized by console type in nested static classes |
| `SequentialTestRunner` | Test runner with file collection and parallel execution support |
| `TestInitializer` | Test assembly initialization |

## Running Tests

```bash
dotnet test
```

Or from the solution root:

```bash
dotnet test SimpleChdDrive.Core.Tests
```

Test CHD files must be placed in the directories specified in `ChdPathCatalog.cs`.

## Dependencies

- **xUnit** 2.9.3 — Test framework
- **xunit.runner.visualstudio** 3.1.5 — VS test adapter
- **Microsoft.NET.Test.Sdk** 18.8.1
- **coverlet.collector** 10.0.1 — Code coverage
- **Serilog** — Logging (for service tests)

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE) file for details.
