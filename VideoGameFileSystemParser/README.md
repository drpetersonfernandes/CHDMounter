# VideoGameFileSystemParser

A **cross-platform .NET library** for parsing video game console disc image file systems. Supports CHD, ISO, and raw sector data across 20+ console formats including PlayStation, Xbox, Dreamcast, CD-i, 3DO, and more.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-VideoGameFileSystemParser-blue)](https://www.nuget.org/)
[![License](https://img.shields.io/badge/License-MIT-green)](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE)

## Installation

```bash
dotnet add package VideoGameFileSystemParser
```

## Features

- Read **CHD** (MAME Compressed Hunks of Data) disc images
- Parse file systems for **20+ console formats**
- Navigate file/directory trees with `ChdContainer`
- Access raw sectors via `SectorReader`
- Multi-extent and interleaved file support
- CD-ROM XA Mode 2 subheader-aware reads
- Virtual CUE/BIN raw disc image export
- Unsafe `ReadOnlySpan<byte>` for high-performance sector parsing

## Supported File Systems

| Console | File System | Parser |
|---------|-------------|--------|
| PlayStation 1 | CD-ROM XA / ISO 9660 | `PlayStation1Parser` |
| PlayStation 2 | ISO 9660 | `PlayStation2Parser` |
| PlayStation 3 | UDF | `PlayStation3Parser` |
| PSP | ISO 9660 (UMD) | `PspParser` |
| Xbox | XDVDFS | `XboxParser` |
| Xbox 360 | XGD / XSF | `Xbox360Parser` |
| Dreamcast | ISO 9660 + IP.BIN | `DreamcastParser` |
| Saturn | ISO 9660 | `SegaSaturnParser` |
| CD-i | CD-i File System | `CDiParser` |
| 3DO | Opera File System | `ThreeDoConsoleParser` |
| Neo Geo CD | ISO 9660 | `NeoGeoCdParser` |
| PC Engine CD | PC Engine CD-ROM | `PcEngineCdParser` |
| PC-FX | PC-FX ISO | `PcFxParser` |
| Amiga CD32 | ISO 9660 | `AmigaCd32Parser` |
| Amiga CD | ISO 9660 | `AmigaCdParser` |
| Sega Genesis CD | ISO 9660 | `SegaGenesisCdParser` |
| Generic ISO 9660 | ISO 9660 | `GenericIso9660Parser` |
| Generic RAW | Raw Sectors | `GenericIsoRawParser` |
| + auto-detect | — | `PlayStationAutoDetectParser` |

## Quick Start

```csharp
using VideoGameFileSystemParser;
using VideoGameFileSystemParser.Parsers;

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
    └── ChdFile (CHDSharp)
    └── SectorReader (raw/cooked sector access)
        └── TrackInfo (multi-track CHD layout)
    └── ParserFactory (ConsoleType → IConsoleParser)
        └── IConsoleParser (Parse / ParseTrack)
            ├── Iso9660Parser  ─→  Iso9660Wrapper  →  Psp, Saturn, NeoGeo, Amiga...
            ├── UdfParser       ─→  PS3
            ├── XdvdfsParser    ─→  Xbox, Xbox 360
            ├── ThreeDoParser   ─→  3DO, CD-i
            ├── CDiFsParser     ─→  CD-i
            └── PlayStationParsers (CD-ROM XA)
```

## Key Types

| Type | Description |
|------|-------------|
| `ChdContainer` | High-level API: open CHD, parse file system, navigate files |
| `SectorReader` | Low-level sector access with track-aware LBA mapping |
| `ParserFactory` | Maps `ConsoleType` to `IConsoleParser` implementations |
| `IConsoleParser` | Contract for console-specific file system parsers |
| `FileEntry` | File/directory in the parsed tree |
| `FsNode` | Raw parser output node with LBA, size, extents, interleaving |
| `TrackInfo` | CHD track metadata (index, type, LBA, offset, pregap, postgap) |

## Dependencies

- **CHDSharp** 1.1.0 — MAME CHD format reader

## License

MIT — see the [repository](https://github.com/drpetersonfernandes/SimpleChdDrive) for details.
