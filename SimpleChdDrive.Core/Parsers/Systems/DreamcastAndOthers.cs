namespace SimpleChdDrive.Core.Parsers.Systems;

public class DreamcastParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public DreamcastParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Dreamcast;
    }

    public string GetConsoleName()
    {
        return "Dreamcast";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new DreamcastIsoParser(_reader);

        var offsets = new[] { -45000, -45150, -150, 0, 45000, 45150 };
        foreach (var offset in offsets)
        {
            parser.SetLbaOffset(offset);
            if (parser.Parse(rootNode, track))
                return true;
        }

        var isoParser = new Iso9660Parser(_reader);
        return isoParser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class CDiParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public CDiParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.CDi;
    }

    public string GetConsoleName()
    {
        return "CD-i";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new CDiFsParser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class ThreeDoConsoleParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public ThreeDoConsoleParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.ThreeDo;
    }

    public string GetConsoleName()
    {
        return "3DO";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new ThreeDoParser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class GenericIso9660Parser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public GenericIso9660Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.GenericIso9660;
    }

    public string GetConsoleName()
    {
        return "Generic ISO 9660";
    }

    public bool Parse(FsNode rootNode)
    {
        var track = FindDataTrack();
        if (track == null)
            return false;

        return ParseTrack(rootNode, track);
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo? FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.FirstOrDefault();
    }
}
