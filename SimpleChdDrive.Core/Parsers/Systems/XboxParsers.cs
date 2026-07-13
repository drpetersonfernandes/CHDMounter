namespace SimpleChdDrive.Core.Parsers.Systems;

public class XboxParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public XboxParser(SectorReader reader) => _reader = reader;

    public ConsoleType GetConsoleType() => ConsoleType.Xbox;
    public string GetConsoleName() => "Xbox";

    public bool Parse(FsNode rootNode) => ParseTrack(rootNode, FindDataTrack());

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new XdvdfsParser(_reader);
        parser.SetTrack(track);
        return parser.Parse(rootNode);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;
        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class Xbox360Parser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public Xbox360Parser(SectorReader reader) => _reader = reader;

    public ConsoleType GetConsoleType() => ConsoleType.Xbox360;
    public string GetConsoleName() => "Xbox 360";

    public bool Parse(FsNode rootNode) => ParseTrack(rootNode, FindDataTrack());

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new XdvdfsParser(_reader);
        parser.SetTrack(track);
        return parser.Parse(rootNode);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;
        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

public class XboxSingleFileParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public XboxSingleFileParser(SectorReader reader) => _reader = reader;

    public ConsoleType GetConsoleType() => ConsoleType.XboxSingleFile;
    public string GetConsoleName() => "Xbox (Single File)";

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
            IsDirectory = false
        });
        return true;
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track) => Parse(rootNode);
}
