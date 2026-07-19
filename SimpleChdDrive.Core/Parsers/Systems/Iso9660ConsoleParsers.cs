namespace SimpleChdDrive.Core.Parsers.Systems;

public class PspParser : Iso9660Wrapper
{
    public PspParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.Psp;
    }

    public override string GetConsoleName()
    {
        return "PSP";
    }
}

public class PcFxParser : IConsoleParser
{
    private readonly SectorReader _reader;
    private readonly PcFxIsoParser _isoParser;

    public PcFxParser(SectorReader reader)
    {
        _reader = reader;
        _isoParser = new PcFxIsoParser(reader);
    }

    public bool ForceMode { get; set; }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.PcFx;
    }

    public string GetConsoleName()
    {
        return "PC-FX";
    }

    public bool Parse(FsNode rootNode)
    {
        foreach (var t in _reader.Tracks)
        {
            if (t.IsDataTrack && _isoParser.Parse(rootNode, t))
                return true;
        }

        return _isoParser.Parse(rootNode, null);
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        return _isoParser.Parse(rootNode, track);
    }
}

public class SegaGenesisCdParser : Iso9660Wrapper
{
    public SegaGenesisCdParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.SegaGenesisCd;
    }

    public override string GetConsoleName()
    {
        return "Sega Genesis CD";
    }
}

public class SegaSaturnParser : Iso9660Wrapper
{
    public SegaSaturnParser(SectorReader reader) : base(reader)
    {
    }

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
    public NeoGeoCdParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.NeoGeoCd;
    }

    public override string GetConsoleName()
    {
        return "NeoGeo CD";
    }
}

public class AmigaCd32Parser : Iso9660Wrapper
{
    public AmigaCd32Parser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.AmigaCd32;
    }

    public override string GetConsoleName()
    {
        return "Amiga CD32";
    }
}

public class AmigaCdParser : Iso9660Wrapper
{
    public AmigaCdParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.AmigaCd;
    }

    public override string GetConsoleName()
    {
        return "Amiga CD";
    }
}

public abstract class Iso9660Wrapper : IConsoleParser
{
    protected SectorReader Reader { get; }
    public bool ForceMode { get; set; }

    protected Iso9660Wrapper(SectorReader reader)
    {
        Reader = reader;
    }

    public abstract ConsoleType GetConsoleType();
    public abstract string GetConsoleName();

    public bool Parse(FsNode rootNode)
    {
        var track = FindDataTrack();
        if (track == null)
            return false;

        return ParseTrack(rootNode, track);
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(Reader);
        return parser.Parse(rootNode, track);
    }

    protected TrackInfo? FindDataTrack()
    {
        foreach (var t in Reader.Tracks)
            if (t.IsDataTrack) return t;

        return Reader.Tracks.FirstOrDefault();
    }
}
