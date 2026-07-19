# SimpleChdDrive.Core.Tests

Integration and unit test project for the SimpleChdDrive solution. Validates file system parsers against real CHD disc images across 20+ console types.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![xUnit](https://img.shields.io/badge/xUnit-2.9.3-green)](https://xunit.net/)

## Test Structure

### Parser Integration Tests (`Parsers/`)

Each parser has dedicated integration tests that open a real `.chd` file, parse its file system, and verify the resulting file/directory tree:

| Test | Console | Parser |
|------|---------|--------|
| `Ps1IntegrationTests` | PlayStation 1 | CD-ROM XA |
| `Ps2IntegrationTests` | PlayStation 2 | ISO 9660 |
| `Ps3IntegrationTests` | PlayStation 3 | UDF |
| `PspIntegrationTests` | PSP | ISO 9660 (UMD) |
| `XboxIntegrationTests` | Xbox | XDVDFS |
| `DreamcastIntegrationTests` | Dreamcast | ISO 9660 + IP.BIN |
| `ThreeDoIntegrationTests` | 3DO | OperaFS |
| `CDiIntegrationTests` / `CDiDiagnosticTests` | CD-i | CD-i FS |
| `AmigaCd32IntegrationTests` | Amiga CD32 | ISO 9660 |
| `AmigaCdIntegrationTests` | Amiga CD | ISO 9660 |
| `PceCdIntegrationTests` | PC Engine CD | PC Engine CD-ROM |
| `PcFxIntegrationTests` / `PcFxDiagnosticTests` | PC-FX | PC-FX ISO |
| `FmTownsIntegrationTests` | FM Towns | ISO 9660 |
| `Pc98IntegrationTests` | PC-98 | ISO 9660 |
| `X68000IntegrationTests` | X68000 | ISO 9660 |

### Test Infrastructure

| Class | Description |
|-------|-------------|
| `ChdPathCatalog` | Centralized catalog of test CHD file paths, organized by console type in nested static classes |
| `SequentialTestRunner` | Test runner with file collection and parallel execution support |

### Unit Tests

| Test | Description |
|------|-------------|
| `LoggingServiceTests` | Validates WPF-dispatched log collection |
| `ServiceProviderTests` | Validates DI container registration and disposal |
| `EnumTests` | ConsoleType enumeration validation |
| `ModelTests` | Model behavior and serialization tests |

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
- **Microsoft.NET.Test.Sdk** 18.8.1
- **coverlet.collector** — Code coverage

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE) file for details.
