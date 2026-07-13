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
            ConsoleType.AmigaCD32 => new AmigaCD32Parser(reader),
            ConsoleType.AmigaCD => new AmigaCDParser(reader),
            ConsoleType.PcEngineCD => new PcEngineCDParser(reader),
            ConsoleType.SegaGenesisCD => new SegaGenesisCDParser(reader),
            ConsoleType.Saturn => new SegaSaturnParser(reader),
            ConsoleType.NeoGeoCD => new NeoGeoCDParser(reader),
            ConsoleType.PcFx => new PcFxParser(reader),
            ConsoleType.GenericISO9660 => new GenericIso9660Parser(reader),
            ConsoleType.GenericCueBin or ConsoleType.GenericCueBin2048 or ConsoleType.Unknown => null,
            _ => null
        };
    }

    public static List<ConsoleInfo> GetAllSupportedConsoles()
    {
        return
        [
            new(ConsoleType.Xbox, "Xbox"),
            new(ConsoleType.Xbox360, "Xbox 360"),
            new(ConsoleType.XboxSingleFile, "Xbox (Single File)"),
            new(ConsoleType.PS1, "PS1"),
            new(ConsoleType.PS2, "PS2"),
            new(ConsoleType.PS3, "PS3"),
            new(ConsoleType.PS3SingleFile, "PS3 (Single File)"),
            new(ConsoleType.PSP, "PSP"),
            new(ConsoleType.Dreamcast, "Dreamcast"),
            new(ConsoleType.CDi, "CD-i"),
            new(ConsoleType.ThreeDO, "3DO"),
            new(ConsoleType.AmigaCD32, "Amiga CD32"),
            new(ConsoleType.AmigaCD, "Amiga CD"),
            new(ConsoleType.PcEngineCD, "PC Engine CD"),
            new(ConsoleType.SegaGenesisCD, "Sega Genesis CD"),
            new(ConsoleType.Saturn, "Saturn"),
            new(ConsoleType.NeoGeoCD, "NeoGeo CD"),
            new(ConsoleType.PcFx, "PC-FX"),
            new(ConsoleType.GenericISO9660, "Generic ISO 9660"),
            new(ConsoleType.GenericCueBin, "CUE/BIN (Raw)"),
            new(ConsoleType.GenericCueBin2048, "CUE/BIN (Cooked)"),
        ];
    }
}
