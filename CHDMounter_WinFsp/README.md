# CHDMounter (WinFsp)

WPF desktop application that mounts CHD (Compressed Hunks of Data) disc images as **read-only virtual drives** in Windows using the [WinFsp](https://github.com/winfsp/winfsp) user-mode file system driver.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![WinFsp](https://img.shields.io/badge/WinFsp-2.2-blue)](https://github.com/winfsp/winfsp)

## Supported Consoles

| # | Console | CLI Alias | File System |
|---|---------|-----------|-------------|
| 1 | Amiga CD | `amigacd`, `amiga` | ISO 9660 |
| 2 | Amiga CD32 | `amigacd32`, `cd32` | ISO 9660 |
| 3 | Amiga CDTV | (GUI only) | ISO 9660 |
| 4 | CD-i | `cdi`, `cd-i` | CD-i Green Book |
| 5 | Dreamcast | `dreamcast`, `dc` | ISO 9660 + IP.BIN |
| 6 | FM Towns | `fmtowns`, `fmt` | ISO 9660 |
| 7 | Generic ISO 9660 | `iso9660`, `generic`, `iso` | ISO 9660 / High Sierra |
| 8 | Generic Raw | (GUI only) | Raw sectors → image.iso |
| 9 | CUE/BIN (2352) | `cuebin`, `cue` | Virtual CUE/BIN |
| 10 | CUE/BIN (2048) | `cuebin2048`, `cue2048` | Virtual CUE/BIN |
| 11 | CUE/ISO | `cueiso` | Virtual CUE/ISO |
| 12 | CUE/BIN/WAV | `cuebinwav`, `cuewav` | Virtual CUE/BIN/WAV |
| 13 | CUE/ISO/WAV | `cueisowav` | Virtual CUE/ISO/WAV |
| 14 | Neo Geo CD | `neogeo`, `ngcd` | ISO 9660 |
| 15 | Nuon | (GUI only) | UDF → ISO 9660 fallback |
| 16 | PC Engine CD | `pcengine`, `pce`, `tgcd` | ISO 9660 |
| 17 | PC-FX | `pcfx` | PC-FX ISO |
| 18 | PC-98 | (GUI only) | ISO 9660 |
| 19 | Pico | `pico` | ISO 9660 |
| 20 | Pippin | (GUI only) | HFS → HFS+ → UDF → ISO |
| 21 | PlayStation (Auto) | `psauto`, `psdetect` | ISO 9660 (auto-detect) |
| 22 | PS1 | `ps1`, `playstation`, `psx` | ISO 9660 |
| 23 | PS2 | `ps2` | ISO 9660 |
| 24 | PS3 | `ps3` | UDF → ISO 9660 fallback |
| 25 | PS3 (Single File) | (GUI only) | Virtual ISO passthrough |
| 26 | PSP | `psp` | ISO 9660 (UMD) |
| 27 | Saturn | `saturn` | ISO 9660 |
| 28 | Sega Genesis CD | `segagenesis`, `megacd`, `segacd` | ISO 9660 |
| 29 | 3DO | `3do` | Opera FS (ISO 9660 fallback) |
| 30 | X68000 | `x68000`, `x68k` | ISO 9660 → UDF fallback |
| 31 | Xbox | `xbox` | XDVDFS |
| 32 | Xbox 360 | `xbox360`, `x360` | XDVDFS |
| 33 | Xbox (Single File) | (GUI only) | Virtual ISO passthrough |

## Usage

```
CHDMounter_WinFsp.exe <chd_file> <console_type> [mount_point]
```

Examples:
```
CHDMounter_WinFsp.exe game.chd ps2 M
CHDMounter_WinFsp.exe game.chd xbox360
CHDMounter_WinFsp.exe disc.chd cuebin
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
| Admin mount | Standard | Cross-integrity folder mounts with permissive DACL |

## Dependencies

- **winfsp.net** 2.2.26194 — WinFsp .NET bindings
- **WPF-UI** 4.3.0 — Modern WPF theming
- **Serilog** — Structured logging
- **VideoGameFileSystemParser** — CHD file system parsing library
- **CHDMounter.Core** — Shared services and interfaces

## Building

```bash
dotnet build -c Release
```

Output is a self-contained single-file executable for `win-x64` and `win-arm64`.

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/CHDMounter/blob/main/LICENSE) file for details.
