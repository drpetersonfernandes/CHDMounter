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
            ConsoleType.PS1 => new PlayStation1Parser(reader),
            ConsoleType.PS2 => new PlayStation2Parser(reader),
            ConsoleType.PS3 => new PlayStation3Parser(reader),
            ConsoleType.PS3SingleFile => new PlayStation3SingleFileParser(reader),
            ConsoleType.PSP => new PspParser(reader),
            ConsoleType.Dreamcast => new DreamcastParser(reader),
            ConsoleType.CDi => new CDiParser(reader),
            ConsoleType.ThreeDO => new ThreeDoConsoleParser(reader),
            ConsoleType.AmigaCD32 => new AmigaCd32Parser(reader),
            ConsoleType.AmigaCD => new AmigaCdParser(reader),
            ConsoleType.PcEngineCD => new PcEngineCdParser(reader),
            ConsoleType.SegaGenesisCD => new SegaGenesisCdParser(reader),
            ConsoleType.Saturn => new SegaSaturnParser(reader),
            ConsoleType.NeoGeoCD => new NeoGeoCdParser(reader),
            ConsoleType.PcFx => new PcFxParser(reader),
            ConsoleType.GenericISO9660 => new GenericIso9660Parser(reader),
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
            new ConsoleInfo(ConsoleType.PS1, "PS1"),
            new ConsoleInfo(ConsoleType.PS2, "PS2"),
            new ConsoleInfo(ConsoleType.PS3, "PS3"),
            new ConsoleInfo(ConsoleType.PS3SingleFile, "PS3 (Single File)"),
            new ConsoleInfo(ConsoleType.PSP, "PSP"),
            new ConsoleInfo(ConsoleType.Dreamcast, "Dreamcast"),
            new ConsoleInfo(ConsoleType.CDi, "CD-i"),
            new ConsoleInfo(ConsoleType.ThreeDO, "3DO"),
            new ConsoleInfo(ConsoleType.AmigaCD32, "Amiga CD32"),
            new ConsoleInfo(ConsoleType.AmigaCD, "Amiga CD"),
            new ConsoleInfo(ConsoleType.PcEngineCD, "PC Engine CD"),
            new ConsoleInfo(ConsoleType.SegaGenesisCD, "Sega Genesis CD"),
            new ConsoleInfo(ConsoleType.Saturn, "Saturn"),
            new ConsoleInfo(ConsoleType.NeoGeoCD, "NeoGeo CD"),
            new ConsoleInfo(ConsoleType.PcFx, "PC-FX"),
            new ConsoleInfo(ConsoleType.GenericISO9660, "Generic ISO 9660"),
            new ConsoleInfo(ConsoleType.GenericCueBin, "CUE/BIN (Raw)"),
            new ConsoleInfo(ConsoleType.GenericCueBin2048, "CUE/BIN (Cooked)")
        ];
    }
}
