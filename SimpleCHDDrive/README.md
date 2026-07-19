# SimpleChdDrive (Dokan)

WPF desktop application that mounts CHD (Compressed Hunks of Data) disc images as **read-only virtual drives** in Windows using the [Dokan](https://github.com/dokan-dev/dokany) file system driver.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![Dokan](https://img.shields.io/badge/Dokan-2.3.0.3-green)](https://github.com/dokan-dev/dokany)

## Supported Consoles

| Console | File System |
|---------|-------------|
| PlayStation 1 | CD-ROM XA / ISO 9660 |
| PlayStation 2 | ISO 9660 |
| PlayStation 3 | UDF |
| PSP | ISO 9660 (UMD) |
| Xbox | XDVDFS |
| Xbox 360 | XGD / XSF |
| Dreamcast | ISO 9660 + IP.BIN |
| Saturn | ISO 9660 |
| CD-i | CD-i File System |
| 3DO | Opera File System |
| Neo Geo CD | ISO 9660 |
| PC Engine CD | PC Engine CD-ROM |
| PC-FX | PC-FX ISO |
| Amiga CD32 / Amiga CD | ISO 9660 |
| Sega Genesis CD | ISO 9660 |
| And more... | |

## Usage

```
SimpleChdDrive.exe <chd_file> <console_type> [mount_point]
```

Examples:
```
SimpleChdDrive.exe game.chd ps2 M
SimpleChdDrive.exe game.chd xbox N
```

Run without arguments to open the GUI for interactive file system type selection.

## Prerequisites

- [Dokan](https://github.com/dokan-dev/dokany/releases) driver installed (version 2.x)
- Windows 10+ (x64 or ARM64)

## Dependencies

- **DokanNet** 2.3.0.3 — Windows virtual filesystem driver
- **WPF-UI** 4.3.0 — Modern WPF theming
- **Serilog** — Structured logging
- **VideoGameFileSystemParser** — CHD file system parsing library
- **SimpleChdDrive.Core** — Shared services and interfaces

## Building

```bash
dotnet build -c Release
```

Output is a self-contained single-file executable for `win-x64` and `win-arm64`.

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE) file for details.
