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

public class PcEngineCDParser : Iso9660Wrapper
{
    public PcEngineCDParser(SectorReader reader) : base(reader) { }
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

public class SegaGenesisCDParser : Iso9660Wrapper
{
    public SegaGenesisCDParser(SectorReader reader) : base(reader) { }
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

public class NeoGeoCDParser : Iso9660Wrapper
{
    public NeoGeoCDParser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.NeoGeoCD;
    }

    public override string GetConsoleName()
    {
        return "NeoGeo CD";
    }
}

public class AmigaCD32Parser : Iso9660Wrapper
{
    public AmigaCD32Parser(SectorReader reader) : base(reader) { }
    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.AmigaCD32;
    }

    public override string GetConsoleName()
    {
        return "Amiga CD32";
    }
}

public class AmigaCDParser : Iso9660Wrapper
{
    public AmigaCDParser(SectorReader reader) : base(reader) { }
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
    protected readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    protected Iso9660Wrapper(SectorReader reader)
    {
        _reader = reader;
    }

    public abstract ConsoleType GetConsoleType();
    public abstract string GetConsoleName();

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(_reader);
        return parser.Parse(rootNode, track);
    }

    protected TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}
