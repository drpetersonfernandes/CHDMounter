namespace SimpleChdDrive.Core.Parsers;

public static class ParserFactory
{
    public static IConsoleParser CreateParser(ConsoleType type, SectorReader reader)
    {
        return type switch
        {
            ConsoleType.Xbox => new XboxParser(reader),
            ConsoleType.Xbox360 => new Xbox360Parser(reader),
            ConsoleType.XboxSingleFile => new XboxSingleFileParser(reader),
            ConsoleType.Ps1 => new PlayStation1Parser(reader),
            ConsoleType.Ps2 => new PlayStation2Parser(reader),
            ConsoleType.Ps3 => new PlayStation3Parser(reader),
            ConsoleType.Ps3SingleFile => new PlayStation3SingleFileParser(reader),
            ConsoleType.Psp => new PspParser(reader),
            ConsoleType.Dreamcast => new DreamcastParser(reader),
            ConsoleType.CDi => new CDiParser(reader),
            ConsoleType.ThreeDo => new ThreeDoConsoleParser(reader),
            ConsoleType.AmigaCd32 => new AmigaCd32Parser(reader),
            ConsoleType.AmigaCd => new AmigaCdParser(reader),
            ConsoleType.PcEngineCd => new PcEngineCdParser(reader),
            ConsoleType.SegaGenesisCd => new SegaGenesisCdParser(reader),
            ConsoleType.Saturn => new SegaSaturnParser(reader),
            ConsoleType.NeoGeoCd => new NeoGeoCdParser(reader),
            ConsoleType.PcFx => new PcFxParser(reader),
            ConsoleType.PlayStation => new PlayStationAutoDetectParser(reader),
            ConsoleType.GenericIso9660 => new GenericIso9660Parser(reader),
            _ => null!
        };
    }

    public static IEnumerable<ConsoleInfo> GetAllSupportedConsoles()
    {
        return
        [
            new ConsoleInfo(ConsoleType.Xbox, "Xbox"),
            new ConsoleInfo(ConsoleType.Xbox360, "Xbox 360"),
            new ConsoleInfo(ConsoleType.XboxSingleFile, "Xbox (Single File)"),
            new ConsoleInfo(ConsoleType.Ps1, "PS1"),
            new ConsoleInfo(ConsoleType.Ps2, "PS2"),
            new ConsoleInfo(ConsoleType.Ps3, "PS3"),
            new ConsoleInfo(ConsoleType.Ps3SingleFile, "PS3 (Single File)"),
            new ConsoleInfo(ConsoleType.PlayStation, "PlayStation (Auto)"),
            new ConsoleInfo(ConsoleType.Psp, "PSP"),
            new ConsoleInfo(ConsoleType.Dreamcast, "Dreamcast"),
            new ConsoleInfo(ConsoleType.CDi, "CD-i"),
            new ConsoleInfo(ConsoleType.ThreeDo, "3DO"),
            new ConsoleInfo(ConsoleType.AmigaCd32, "Amiga CD32"),
            new ConsoleInfo(ConsoleType.AmigaCd, "Amiga CD"),
            new ConsoleInfo(ConsoleType.PcEngineCd, "PC Engine CD"),
            new ConsoleInfo(ConsoleType.SegaGenesisCd, "Sega Genesis CD"),
            new ConsoleInfo(ConsoleType.Saturn, "Saturn"),
            new ConsoleInfo(ConsoleType.NeoGeoCd, "NeoGeo CD"),
            new ConsoleInfo(ConsoleType.PcFx, "PC-FX"),
            new ConsoleInfo(ConsoleType.GenericIso9660, "Generic ISO 9660"),
            new ConsoleInfo(ConsoleType.GenericCueBin, "CUE/BIN (Raw)"),
            new ConsoleInfo(ConsoleType.GenericCueBin2048, "CUE/BIN (Cooked)")
        ];
    }
}
