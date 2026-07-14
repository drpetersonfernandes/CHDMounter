namespace SimpleChdDrive.Core.Parsers.Systems;

public class PlayStation1Parser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public PlayStation1Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Ps1;
    }

    public string GetConsoleName()
    {
        return "PS1";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class PlayStationAutoDetectParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public PlayStationAutoDetectParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.PlayStation;
    }

    public string GetConsoleName()
    {
        return "PlayStation (Auto)";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class PlayStation2Parser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public PlayStation2Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Ps2;
    }

    public string GetConsoleName()
    {
        return "PS2";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(_reader);
        return parser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class PlayStation3Parser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public PlayStation3Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Ps3;
    }

    public string GetConsoleName()
    {
        return "PS3";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var udfParser = new UdfParser(_reader);
        if (udfParser.Parse(rootNode, track))
            return true;

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

public class PlayStation3SingleFileParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public PlayStation3SingleFileParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Ps3SingleFile;
    }

    public string GetConsoleName()
    {
        return "PS3 (Single File)";
    }

    public bool Parse(FsNode rootNode)
    {
        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = 0;
        rootNode.Children.Add(new FsNode
        {
            Name = "image.iso",
            Lba = 0,
            Size = _reader.TotalBytes,
            IsDirectory = false,
            IsRawPassthrough = true
        });
        return true;
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        return Parse(rootNode);
    }
}
