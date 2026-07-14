namespace SimpleChdDrive.Core.Parsers.Systems;

public class PspParser : Iso9660Wrapper
{
    public PspParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.PSP;
    }

    public override string GetConsoleName()
    {
        return "PSP";
    }
}

public class PcEngineCdParser : Iso9660Wrapper
{
    public PcEngineCdParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.PcEngineCD;
    }

    public override string GetConsoleName()
    {
        return "PC Engine CD";
    }
}

public class PcFxParser : Iso9660Wrapper
{
    public PcFxParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.PcFx;
    }

    public override string GetConsoleName()
    {
        return "PC-FX";
    }
}

public class SegaGenesisCdParser : Iso9660Wrapper
{
    public SegaGenesisCdParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.SegaGenesisCD;
    }

    public override string GetConsoleName()
    {
        return "Sega Genesis CD";
    }
}

public class SegaSaturnParser : Iso9660Wrapper
{
    public SegaSaturnParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.Saturn;
    }

    public override string GetConsoleName()
    {
        return "Saturn";
    }
}

public class NeoGeoCdParser : Iso9660Wrapper
{
    public NeoGeoCdParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.NeoGeoCD;
    }

    public override string GetConsoleName()
    {
        return "NeoGeo CD";
    }
}

public class AmigaCd32Parser : Iso9660Wrapper
{
    public AmigaCd32Parser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.AmigaCD32;
    }

    public override string GetConsoleName()
    {
        return "Amiga CD32";
    }
}

public class AmigaCdParser : Iso9660Wrapper
{
    public AmigaCdParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.AmigaCD;
    }

    public override string GetConsoleName()
    {
        return "Amiga CD";
    }
}

public abstract class Iso9660Wrapper : IConsoleParser
{
    protected readonly SectorReader Reader;
    public bool ForceMode { get; set; }

    protected Iso9660Wrapper(SectorReader reader)
    {
        Reader = reader;
    }

    public abstract ConsoleType GetConsoleType();
    public abstract string GetConsoleName();

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(Reader);
        return parser.Parse(rootNode, track);
    }

    protected TrackInfo FindDataTrack()
    {
        foreach (var t in Reader.Tracks)
            if (t.IsDataTrack) return t;

        return Reader.Tracks.Count > 0 ? Reader.Tracks[0] : new TrackInfo();
    }
}
