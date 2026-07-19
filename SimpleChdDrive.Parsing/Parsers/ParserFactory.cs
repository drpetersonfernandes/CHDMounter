using SimpleChdDrive.Parsing.Interfaces;
using SimpleChdDrive.Parsing.Parsers.Systems;

namespace SimpleChdDrive.Parsing.Parsers;

public static class ParserFactory
{
    public static IConsoleParser CreateParser(ConsoleType type, SectorReader reader)
    {
        return type switch
        {
            ConsoleType.AmigaCd => new AmigaCdParser(reader),
            ConsoleType.AmigaCd32 => new AmigaCd32Parser(reader),
            ConsoleType.CDi => new CDiParser(reader),
            ConsoleType.Dreamcast => new DreamcastParser(reader),
            ConsoleType.GenericIso9660 => new GenericIso9660Parser(reader),
            ConsoleType.GenericIsoRaw => new GenericIsoRawParser(reader),
            ConsoleType.NeoGeoCd => new NeoGeoCdParser(reader),
            ConsoleType.PcEngineCd => new PcEngineCdParser(reader),
            ConsoleType.PcFx => new PcFxParser(reader),
            ConsoleType.PlayStation => new PlayStationAutoDetectParser(reader),
            ConsoleType.Ps1 => new PlayStation1Parser(reader),
            ConsoleType.Ps2 => new PlayStation2Parser(reader),
            ConsoleType.Ps3 => new PlayStation3Parser(reader),
            ConsoleType.Psp => new PspParser(reader),
            ConsoleType.Saturn => new SegaSaturnParser(reader),
            ConsoleType.SegaGenesisCd => new SegaGenesisCdParser(reader),
            ConsoleType.ThreeDo => new ThreeDoConsoleParser(reader),
            ConsoleType.Xbox => new XboxParser(reader),
            ConsoleType.Xbox360 => new Xbox360Parser(reader),
            _ => null!
        };
    }

    public static IEnumerable<ConsoleInfo> GetAllSupportedConsoles()
    {
        return
        [
            new ConsoleInfo(ConsoleType.Unknown, "Unknown"),
            new ConsoleInfo(ConsoleType.AmigaCd, "Amiga CD"),
            new ConsoleInfo(ConsoleType.AmigaCd32, "Amiga CD32"),
            new ConsoleInfo(ConsoleType.CDi, "CD-i"),
            new ConsoleInfo(ConsoleType.GenericIso9660, "Generic ISO 9660"),
            new ConsoleInfo(ConsoleType.GenericIsoRaw, "Generic ISO Raw"),
            new ConsoleInfo(ConsoleType.GenericCueBin2352Default, "CUE/BIN (2352 Default)"),
            new ConsoleInfo(ConsoleType.GenericCueBin2048, "CUE/BIN (Cooked)"),
            new ConsoleInfo(ConsoleType.GenericCueIso, "CUE/ISO (2048)"),
            new ConsoleInfo(ConsoleType.GenericCueBinWav, "CUE/BIN/WAV"),
            new ConsoleInfo(ConsoleType.GenericCueIsoWav, "CUE/ISO/WAV"),
            new ConsoleInfo(ConsoleType.Dreamcast, "Dreamcast"),
            new ConsoleInfo(ConsoleType.NeoGeoCd, "NeoGeo CD"),
            new ConsoleInfo(ConsoleType.PcEngineCd, "PC Engine CD"),
            new ConsoleInfo(ConsoleType.PcFx, "PC-FX"),
            new ConsoleInfo(ConsoleType.PlayStation, "PlayStation (Auto)"),
            new ConsoleInfo(ConsoleType.Ps1, "PS1"),
            new ConsoleInfo(ConsoleType.Ps2, "PS2"),
            new ConsoleInfo(ConsoleType.Ps3, "PS3"),
            new ConsoleInfo(ConsoleType.Psp, "PSP"),
            new ConsoleInfo(ConsoleType.Saturn, "Saturn"),
            new ConsoleInfo(ConsoleType.SegaGenesisCd, "Sega Genesis CD"),
            new ConsoleInfo(ConsoleType.ThreeDo, "3DO"),
            new ConsoleInfo(ConsoleType.Xbox, "Xbox"),
            new ConsoleInfo(ConsoleType.Xbox360, "Xbox 360")
        ];
    }
}
