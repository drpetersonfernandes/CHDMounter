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

    public static List<(ConsoleType Type, string Name)> GetAllSupportedConsoles()
    {
        return
        [
            (ConsoleType.Xbox, "Xbox"),
            (ConsoleType.Xbox360, "Xbox 360"),
            (ConsoleType.XboxSingleFile, "Xbox (Single File)"),
            (ConsoleType.PS1, "PS1"),
            (ConsoleType.PS2, "PS2"),
            (ConsoleType.PS3, "PS3"),
            (ConsoleType.PS3SingleFile, "PS3 (Single File)"),
            (ConsoleType.PSP, "PSP"),
            (ConsoleType.Dreamcast, "Dreamcast"),
            (ConsoleType.CDi, "CD-i"),
            (ConsoleType.ThreeDO, "3DO"),
            (ConsoleType.AmigaCD32, "Amiga CD32"),
            (ConsoleType.AmigaCD, "Amiga CD"),
            (ConsoleType.PcEngineCD, "PC Engine CD"),
            (ConsoleType.SegaGenesisCD, "Sega Genesis CD"),
            (ConsoleType.Saturn, "Saturn"),
            (ConsoleType.NeoGeoCD, "NeoGeo CD"),
            (ConsoleType.PcFx, "PC-FX"),
            (ConsoleType.GenericISO9660, "Generic ISO 9660"),
            (ConsoleType.GenericCueBin, "CUE/BIN (Raw)"),
            (ConsoleType.GenericCueBin2048, "CUE/BIN (Cooked)"),
        ];
    }
}
