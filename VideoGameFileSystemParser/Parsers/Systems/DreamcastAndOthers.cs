using System.Text;
using VideoGameFileSystemParser.Interfaces;

namespace VideoGameFileSystemParser.Parsers.Systems;

public class DreamcastParser : IConsoleParser
{
    private const string IpBinSignature = "SEGA SEGAKATANA ";

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
        var dataTracks = new List<TrackInfo>();
        for (var i = _reader.Tracks.Count - 1; i >= 0; i--)
        {
            if (_reader.Tracks[i].IsDataTrack)
                dataTracks.Add(_reader.Tracks[i]);
        }

        if (dataTracks.Count == 0)
            return false;

        foreach (var track in dataTracks.OrderByDescending(HasIpBin))
        {
            if (ParseTrack(rootNode, track))
                return true;
        }

        return false;
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var temp = new FsNode();
        var parser = new Iso9660Parser(_reader);

        if (!parser.Parse(temp, track) || temp.Children.Count == 0)
            return false;

        rootNode.Name = temp.Name;
        rootNode.IsDirectory = true;
        rootNode.Lba = temp.Lba;
        rootNode.Size = temp.Size;
        rootNode.Extents.Clear();
        rootNode.Extents.AddRange(temp.Extents);
        rootNode.Children.Clear();
        rootNode.Children.AddRange(temp.Children);
        return true;
    }

    private bool HasIpBin(TrackInfo track)
    {
        _reader.Reset();
        _reader.SetTrack(track, true);

        var sec = new byte[2048];
        var ok = _reader.ReadSector(track.StartLba, sec) &&
                 Encoding.ASCII.GetString(sec, 0, IpBinSignature.Length) == IpBinSignature;

        _reader.Reset();
        return ok;
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
        if (parser.Parse(rootNode, track))
            return true;

        var isoParser = new Iso9660Parser(_reader);
        if (isoParser.Parse(rootNode, track))
            return true;

        return false;
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

public class GenericIsoRawParser : IConsoleParser
{
    private readonly SectorReader _reader;
    public bool ForceMode { get; set; }

    public GenericIsoRawParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.GenericIsoRaw;
    }

    public string GetConsoleName()
    {
        return "Generic ISO Raw";
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
