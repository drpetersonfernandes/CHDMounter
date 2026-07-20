# SimpleChdDrive

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://www.microsoft.com/windows)
[![Arch](https://img.shields.io/badge/arch-x64%20%7C%20ARM64-lightgrey)](#)

Mount CHD (Compressed Hunks of Data) CD/DVD images as virtual read-only drives on Windows. Supports 22 filesystem types across consoles and PC optical media.

<p align="center">
  <img src=".github/screenshot.png" alt="Screenshot" width="700">
</p>

---

## Features

- **Mount CHD files as virtual drives** using [Dokan](https://github.com/dokan-dev/dokany) or [WinFsp](https://winfsp.dev/)
- **22 filesystem types** — ISO 9660, UDF, XDVDFS (Xbox), OperaFS (3DO), CD-i Green Book, and more
- **Virtual CUE/BIN export** — presents CD images as `.cue` + `.bin` files for emulators and burning tools
- **SingleFile ISO passthrough** — single `image.iso` file for emulators that expect raw ISO (xemu, RPCS3)
- **Read-only** — never modifies your CHD files
- **Automatic filesystem detection by track header** — no manual setup needed for most discs
- **Command-line interface** for scripting and frontend integration
- **WPF dark theme UI** with console type dropdown, real-time log output
- **Serilog** structured logging to file and debug output
- **Single-file self-contained publish** — distribute as one `.exe`
- **x64 and ARM64** support

---

## Supported Filesystems

| Console / Format          | CLI Alias                          | Filesystem                | Notes                                        |
|---------------------------|------------------------------------|---------------------------|----------------------------------------------|
| PS1                       | `ps1`, `playstation`, `psx`        | ISO 9660                  | Track-level parsing, aggressive PVD scan     |
| PS2                       | `ps2`                              | ISO 9660                  | CD/DVD auto-detection                        |
| PS3                       | `ps3`                              | UDF → ISO 9660 fallback   | Multi-extent large file support              |
| PS3 (Single File)         | GUI only                           | Virtual ISO passthrough   | Single `image.iso` for RPCS3                 |
| PSP                       | `psp`                              | ISO 9660                  | UMD image support                            |
| Xbox                      | `xbox`                             | XDVDFS                    | Binary tree directory structure              |
| Xbox 360                  | `xbox360`, `x360`                  | XDVDFS                    | XGD2/XGD3 offset detection                   |
| Xbox (Single File)        | GUI only                           | Virtual ISO passthrough   | Single `image.iso` for xemu                  |
| Dreamcast                 | `dreamcast`, `dc`                  | ISO 9660                  | GD-ROM offset search (-45000, -150, 0, etc.) |
| 3DO                       | `3do`                              | OperaFS                   | Block-based directory chain                  |
| CD-i                      | `cdi`, `cd-i`                      | Green Book                | Interleaved stream support, Path Table        |
| Saturn                    | `saturn`                           | ISO 9660                  |                                              |
| NeoGeo CD                 | `neogeo`, `ngcd`                   | ISO 9660                  |                                              |
| PC Engine CD              | `pcengine`, `pce`, `tgcd`          | ISO 9660                  |                                              |
| Sega Genesis / Mega CD    | `segagenesis`, `megacd`, `segacd`  | ISO 9660                  |                                              |
| PC-FX                     | `pcfx`                             | ISO 9660                  |                                              |
| Amiga CD32                | `amigacd32`, `amiga`               | ISO 9660                  |                                              |
| Amiga CD                  | `amigacd`                          | ISO 9660                  |                                              |
| Generic ISO 9660          | `iso9660`, `generic`, `iso`        | ISO 9660 / High Sierra    | Joliet/UTF-16BE filename support             |
| CUE/BIN (Raw)             | `cuebin`, `cue`                    | Virtual                   | Raw 2352-byte sectors                        |
| CUE/BIN (Cooked)          | GUI only                           | Virtual                   | 2048-byte sectors                            |

---

## Requirements

- **Windows 10 or later** (x64 or ARM64)
- **.NET 10.0 Desktop Runtime** (or self-contained build ships its own)
- **Dokan** v2.x driver — [install Dokany](https://github.com/dokan-dev/dokany/releases)  
  or
- **WinFsp** 2024+ — [download WinFsp](https://winfsp.dev/rel/)

> Only one driver is needed. The Dokan executable uses Dokan; the WinFsp executable uses WinFsp. Both ship side-by-side.

---

## Quick Start

### Download

Grab the latest self-contained executable from [Releases](https://github.com/your/repo/releases):

- `SimpleChdDrive.exe` — Dokan-based
- `SimpleChdDrive_WinFsp.exe` — WinFsp-based

No installation required. Just download and run.

### GUI Mode

```
SimpleChdDrive.exe
```

Opens the main window. Click **Browse** to select a CHD file, pick a filesystem type from the dropdown, and click **Mount**. The drive appears in Explorer.

### Command Line

```
SimpleChdDrive.exe <chd_file> <console_type> [mount_point]
```

**Examples:**
```bash
# Mount a PS2 game as drive M:
SimpleChdDrive.exe game.chd ps2 M

# Mount an Xbox 360 game (auto-select drive letter)
SimpleChdDrive.exe game.chd xbox360

# Mount as virtual CUE/BIN
SimpleChdDrive.exe disc.chd cuebin

# Mount with generic ISO 9660 parser
SimpleChdDrive_WinFsp.exe data.chd iso9660 N
```

If `<console_type>` is omitted in GUI mode, a dialog appears asking you to choose.

---

## Project Structure

```
SimpleChdDrive.sln
├── SimpleChdDrive.Core/               Shared library
│   ├── CHD/                            MAME CHD reading (CHDSharpLib)
│   │   ├── CHDFile.cs                  Public API — open, read hunks/bytes
│   │   ├── CHDHeaders.cs               Header parsers for CHD V1-V5
│   │   ├── CHDReaders.cs               Decompressor delegates (zlib, LZMA, FLAC, Huffman, Zstd)
│   │   ├── Flac/                       CUETools FLAC decoder
│   │   ├── LZMA/                       7-Zip SDK LZMA decoder
│   │   └── Utils/                      BigEndian, CRC, Huffman, cdRom ECC
│   ├── Parsers/                        Filesystem parsers (ported from C++ CHDMounter)
│   │   ├── Iso9660Parser.cs            ISO 9660 / High Sierra / Joliet
│   │   ├── DreamcastIsoParser.cs       GD-ROM specific ISO parser
│   │   ├── XdvdfsParser.cs             Xbox XDVDFS binary-tree parser
│   │   ├── ThreeDoParser.cs            3DO OperaFS block-based parser
│   │   ├── CDiFsParser.cs              CD-i Green Book with interleaving
│   │   ├── UdfParser.cs                UDF 1.02-2.60 (PS3/DVD/Blu-ray)
│   │   ├── SectorReader.cs             LBA→CHD frame mapper + sector header detection
│   │   ├── ChdContainer.cs             File-tree→VFS bridge + virtual CUE/BIN
│   │   ├── ParserFactory.cs            ConsoleType → parser dispatch
│   │   └── Systems/                    21 console wrapper classes
│   ├── Services/                       DI container, logging, settings, mount interface
│   ├── Logging/                        Serilog setup, diagnostic logger, error handler
│   ├── Views/                          Shared UI (ConsoleSelectionWindow, AboutWindow)
│   └── AppTheme.xaml                   Dark theme ResourceDictionary
│
├── SimpleChdDrive/                     WPF EXE — Dokan
│   ├── ChdFs.cs                        IDokanOperations VFS implementation
│   ├── Services/MountService.cs        Dokan mount/unmount lifecycle
│   ├── App.xaml / App.xaml.cs          Entry point, service registration, args
│   └── MainWindow.xaml / .cs           UI with menu, log, status bar
│
└── SimpleChdDrive_WinFsp/              WPF EXE — WinFsp
    ├── ChdFs.cs                        FileSystemBase VFS implementation
    ├── Services/MountService.cs        WinFsp mount/unmount lifecycle
    ├── App.xaml / App.xaml.cs          Entry point (includes WinFsp PATH fix)
    └── MainWindow.xaml / .cs           Same UI as Dokan variant
```

---

## Build from Source

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10+ (x64 or ARM64)

### Build

```bash
git clone https://github.com/your/repo.git
cd repo
dotnet build -c Release
```

Artifacts appear in `SimpleChdDrive\bin\Release\net10.0-windows\win-x64\` and `SimpleChdDrive_WinFsp\bin\Release\...`.

### Publish (single-file self-contained)

```bash
dotnet publish SimpleChdDrive\SimpleChdDrive.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish SimpleChdDrive_WinFsp\SimpleChdDrive_WinFsp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# ARM64
dotnet publish SimpleChdDrive\SimpleChdDrive.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true
```

---

## How It Works

### CHD Reading

CHD (Compressed Hunks of Data) is MAME's lossless compression format for CD/DVD/HDD images. SimpleChdDrive embeds a port of MAME's `libchdr` as CHDSharpLib, supporting all five CHD versions (V1-V5) and all compression codecs:

- **General**: zlib (deflate), LZMA, FLAC (headerless, 16-bit stereo), dynamic Huffman, Zstd
- **CD-sector**: CDZL (zlib), CDLZ (LZMA), CDFL (FLAC), CDZS (Zstd) — with ECC regeneration
- **AV**: AVHuff — audio+video Huffman for laserdisc/arcade captures

### Sector Reading

The `SectorReader` maps logical block addresses (LBAs) to byte offsets within CHD hunks. It:

1. Parses track metadata (`CHT2`/`CHTR`/`CHGD` tags) for multi-track CD images
2. Maps LBA to CHD frame number using track offsets (handles GD-ROM 45000-LBA shift)
3. Reads and caches the compressed hunk via `ReadHunk()`
4. Detects sector mode (Mode 1 = 16-byte header, Mode 2 = 24-byte header) by scanning for the CD sync pattern `00 FF×10 00`
5. Strips headers to deliver 2048 bytes of user data per sector

### File System Parsing

Each parser reads raw 2048-byte sectors and reconstructs the directory tree in a `FsNode` hierarchy:

| Parser            | Magic / Signature              | Entry Size | Key Field Offsets                               |
|-------------------|--------------------------------|------------|------------------------------------------------|
| ISO 9660          | `CD001` / `CDROM` at sector 16 | 34 bytes   | LBA:2, Size:10, Flags:25, Name:33              |
| XDVDFS (Xbox)     | `MICROSOFT*XBOX*MEDIA`         | 14+ bytes  | Left:0, Right:2, Sector:4, Size:8, Attr:12     |
| OperaFS (3DO)     | `01 5A 5A 5A 5A 5A 01`        | 0x48+      | Flags:0, BlockSize:4, Name:32, Avatars:64      |
| CD-i (Green Book) | `CD-I ` / `CD-RTOS` / `CD001`  | 34+ bytes  | Extended attrs with file_number for interleaving |
| UDF               | AVDP at sector 256, Tag ID=2   | variable   | ShortAd:8, LongAd:16 extent descriptors        |

### VFS Operations

The `ChdContainer` bridges the parsed `FsNode` tree to the Dokan/WinFsp VFS layer:

- **Dokan**: `ChdFs` implements `IDokanOperations` — `CreateFile` resolves paths, `ReadFile` maps file offsets to sector reads, `FindFiles` lists directories, `GetVolumeInformation` reports "CHDFS" read-only volume.
- **WinFsp**: `ChdFs` extends `FileSystemBase` — `Open` resolves paths and returns `FileEntry` as both file node and descriptor, `Read` copies sector data into native memory via `Marshal.Copy`, `ReadDirectoryEntry` enumerates children with `.` and `..`.

For CUE/BIN mode, the container generates virtual `.cue` and `.bin` entries dynamically:
- The `.cue` file contains standard `FILE`, `TRACK`, and `INDEX` descriptors built from CHD track metadata.
- The `.bin` file maps reads to raw sector data (2352 or 2048 bytes per sector) using the locked track context for correct LBA calculation.

For SingleFile mode, the container serves the entire decompressed CHD image as a single `image.iso` file via `CHDFile.Read()`.

---

## NuGet Dependencies

| Package              | Version    | Purpose                                   |
|----------------------|------------|-------------------------------------------|
| `DokanNet`           | 2.3.0.3    | Dokan virtual filesystem driver bindings   |
| `winfsp.net`         | 2.2.26194  | WinFsp virtual filesystem driver bindings  |
| `Serilog`            | 4.4.0      | Structured logging                         |
| `Serilog.Sinks.File` | 7.0.0      | File-based log output                      |
| `Serilog.Sinks.Debug`| 3.0.0      | Visual Studio debug output logging         |
| `ZstdSharp.Port`     | 0.8.8      | Zstandard compression (CHD codec)          |

---

## Acknowledgments

- **CHDSharp library** — https://github.com/drpetersonfernandes/CHDSharp
- **MAME** — https://github.com/mamedev/mame

---

## License

[MIT](LICENSE)
