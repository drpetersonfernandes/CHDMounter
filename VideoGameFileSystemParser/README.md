# VideoGameFileSystemParser

A **cross-platform .NET library** for parsing video game console disc image file systems. Supports CHD, ISO, and raw sector data across 31 console formats including PlayStation, Xbox, Dreamcast, CD-i, 3DO, Pippin (HFS), and more.

[![NuGet](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-VideoGameFileSystemParser-blue)](https://www.nuget.org/)
[![License](https://img.shields.io/badge/License-MIT-green)](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE)

## Installation

```bash
dotnet add package VideoGameFileSystemParser
```

## Features

- Read **CHD** (MAME Compressed Hunks of Data) disc images (V1-V5, all codecs)
- Parse file systems for **31 console formats**
- Navigate file/directory trees with `ChdContainer`
- Access raw sectors via `SectorReader`
- Multi-extent and interleaved file support
- CD-ROM XA Mode 2 subheader-aware reads
- Virtual CUE/BIN/ISO/WAV disc image export (5 modes)
- POSIX symlink and permission support via Rock Ridge (SUSP)
- HFS/HFS+ catalog B-tree parsing for Apple Pippin
- Unsafe `ReadOnlySpan<byte>` for high-performance sector parsing
- Multi-target: `net8.0`, `net9.0`, `net10.0` (cross-platform, no WPF dependency)

## Supported File Systems

| Console | File System | Parser Class |
|---------|-------------|--------------|
| PlayStation 1 | ISO 9660 | `PlayStation1Parser` |
| PlayStation 2 | ISO 9660 | `PlayStation2Parser` |
| PlayStation 3 | UDF → ISO 9660 fallback | `PlayStation3Parser` |
| PlayStation (Auto) | ISO 9660 (auto-detect) | `PlayStationAutoDetectParser` |
| PSP | ISO 9660 (UMD) | `PspParser` |
| Xbox | XDVDFS | `XboxParser` |
| Xbox 360 | XDVDFS | `Xbox360Parser` |
| Dreamcast | ISO 9660 + IP.BIN | `DreamcastParser` |
| Saturn | ISO 9660 | `SegaSaturnParser` |
| CD-i | CD-i Green Book | `CDiParser` |
| 3DO | Opera FS (ISO 9660 fallback) | `ThreeDoConsoleParser` |
| Neo Geo CD | ISO 9660 | `NeoGeoCdParser` |
| PC Engine CD | ISO 9660 | `PcEngineCdParser` |
| PC-FX | PC-FX ISO | `PcFxParser` |
| Amiga CD32 | ISO 9660 | `AmigaCd32Parser` |
| Amiga CD / CDTV | ISO 9660 | `AmigaCdParser` |
| Sega Genesis CD | ISO 9660 | `SegaGenesisCdParser` |
| FM Towns | ISO 9660 | `FmTownsParser` |
| X68000 | ISO 9660 → UDF fallback | `X68000Parser` |
| PC-98 | ISO 9660 | `Pc98Parser` |
| Nuon | UDF → ISO 9660 fallback | `NuonParser` |
| Pico | ISO 9660 | `PicoParser` |
| Pippin | HFS → HFS+ → UDF → ISO | `PippinParser` |
| Generic ISO 9660 | ISO 9660 / High Sierra | `GenericIso9660Parser` |
| Generic Raw | Raw sectors | `GenericIsoRawParser` |

## Quick Start

```csharp
using VideoGameFileSystemParser;
using VideoGameFileSystemParser.Models;

// Open a CHD file
using var container = new ChdContainer("game.chd");

// Parse with console type
if (container.Open(ConsoleType.Ps2))
{
    Console.WriteLine($"Volume: {container.VolumeName}");
    Console.WriteLine($"Size: {container.VolumeSize} bytes");

    // List root directory
    foreach (var entry in container.ListDirectory(""))
    {
        Console.WriteLine($"  {(entry.IsDirectory ? "[DIR]" : "[FILE]")} {entry.Name}");
    }

    // Read a file
    var file = container.FindFile("SYSTEM.CNF");
    if (file != null)
    {
        var buffer = new byte[file.Size];
        container.ReadFile(file, 0, buffer, 0, buffer.Length);
    }
}
```

## Architecture

```
ChdContainer (high-level API)
    ├── ChdFile (CHDSharp — CHD V1-V5 reader)
    ├── SectorReader (raw/cooked sector access, hunk caching)
    │   └── TrackInfo (multi-track CHD layout)
    ├── ParserFactory (ConsoleType → IConsoleParser)
    │   └── IConsoleParser (Parse / ParseTrack)
    │       ├── Iso9660Parser  ─→  Iso9660Wrapper → PSP, Saturn, NeoGeo, Amiga, FM Towns, X68000, PC-98, Pico...
    │       ├── UdfParser      ─→  PS3, Nuon
    │       ├── XdvdfsParser   ─→  Xbox, Xbox 360
    │       ├── ThreeDoParser  ─→  3DO
    │       ├── CDiFsParser    ─→  CD-i
    │       ├── HfsParser      ─→  Pippin (HFS/HFS+)
    │       ├── PcFxIsoParser  ─→  PC-FX
    │       └── PlayStationParsers (CD-ROM XA)
    └── Virtual exports: CUE/BIN, CUE/ISO, CUE/BIN/WAV, CUE/ISO/WAV, SingleFile ISO
```

## Key Types

| Type | Description |
|------|-------------|
| `ChdContainer` | High-level API: open CHD, parse file system, navigate files, virtual export |
| `SectorReader` | Low-level sector access with track-aware LBA mapping and hunk caching |
| `ParserFactory` | Maps `ConsoleType` to `IConsoleParser` implementations |
| `IConsoleParser` | Contract for console-specific file system parsers |
| `FileEntry` | Flat file/directory entry with Name, Path, LBA, Size, Extents |
| `FsNode` | Tree node with LBA, size, extents, symlinks, POSIX attributes (uid/gid/mode), timestamps |
| `TrackInfo` | CHD track metadata (index, type, LBA, offset, pregap, postgap) |
| `ConsoleType` | Enum of 31 supported console types |
| `ConsoleInfo` | `record ConsoleInfo(ConsoleType Type, string Name)` |

## Dependencies

- **CHDSharp** 1.2.0 — MAME CHD format reader
- **Microsoft.SourceLink.GitHub** — Source link for NuGet debugging

## License

MIT — see the [repository](https://github.com/drpetersonfernandes/SimpleChdDrive) for details.
