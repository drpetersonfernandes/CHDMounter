# SimpleChdDrive (WinFsp)

WPF desktop application that mounts CHD (Compressed Hunks of Data) disc images as **read-only virtual drives** in Windows using the [WinFsp](https://github.com/winfsp/winfsp) user-mode file system driver.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![WinFsp](https://img.shields.io/badge/WinFsp-2.2-blue)](https://github.com/winfsp/winfsp)

## Supported Consoles

| # | Console | File System |
|---|---------|-------------|
| 1 | Amiga CD | ISO 9660 |
| 2 | Amiga CD32 | ISO 9660 |
| 3 | CD-i | CD-i File System |
| 4 | Generic ISO 9660 | ISO 9660 |
| 5 | Generic ISO Raw | Raw Sectors |
| 6 | CUE/BIN (2352 Default) | CUE/BIN (2352 bytes/sector) |
| 7 | CUE/BIN (Cooked) | CUE/BIN (2048 bytes/sector) |
| 8 | CUE/ISO (2048) | CUE/ISO (2048 bytes/sector) |
| 9 | CUE/BIN/WAV | CUE/BIN with WAV audio |
| 10 | CUE/ISO/WAV | CUE/ISO with WAV audio |
| 11 | Dreamcast | ISO 9660 + IP.BIN |
| 12 | Neo Geo CD | ISO 9660 |
| 13 | PC Engine CD | PC Engine CD-ROM |
| 14 | PC-FX | PC-FX ISO |
| 15 | PlayStation (Auto) | Auto-detect (PS1/PS2/PS3/PSP) |
| 16 | PS1 | CD-ROM XA / ISO 9660 |
| 17 | PS2 | ISO 9660 |
| 18 | PS3 | UDF |
| 19 | PSP | ISO 9660 (UMD) |
| 20 | Saturn | ISO 9660 |
| 21 | Sega Genesis CD | ISO 9660 |
| 22 | 3DO | Opera File System |
| 23 | Xbox | XDVDFS |
| 24 | Xbox 360 | XGD / XSF |

## Usage

```
SimpleChdDrive_WinFsp.exe <console_type> <chd_file>
```

Examples:
```
SimpleChdDrive_WinFsp.exe 17 game.chd
SimpleChdDrive_WinFsp.exe 23 game.chd
```

Run without arguments to open the GUI for interactive file system type selection.

## Prerequisites

- [WinFsp](https://github.com/winfsp/winfsp/releases) installed (2024+ recommended)
- Windows 10+ (x64 or ARM64)

The application automatically locates the WinFsp installation via the Windows registry and adds it to `PATH`.

## Differences from Dokan Variant

| Aspect | Dokan | WinFsp |
|--------|-------|--------|
| Driver | Kernel-mode (Dokan.sys) | User-mode (WinFsp) |
| NuGet | `DokanNet` 2.3.0.3 | `winfsp.net` 2.2.26194 |
| FileSystem base | `IDokanOperations` | `FileSystemBase` |

## Dependencies

- **winfsp.net** 2.2.26194 — WinFsp .NET bindings
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
