using VideoGameFileSystemParser.Interfaces;

namespace VideoGameFileSystemParser.Parsers.Systems;

/// <summary>
/// Parses Sony PSP disc images using ISO 9660 on the first data track.
/// </summary>
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

/// <summary>
/// Parses NEC PC-FX disc images using the dedicated PcFxIsoParser.
/// </summary>
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

    /// <summary>
/// Parses the first data track found in the reader using ISO 9660.
/// </summary>
/// <param name="rootNode">The root FsNode to populate.</param>
/// <returns>true if parsing succeeded.</returns>
    public bool Parse(FsNode rootNode)
    {
        foreach (var t in _reader.Tracks)
        {
            if (t.IsDataTrack && _isoParser.Parse(rootNode, t))
                return true;
        }

        return _isoParser.Parse(rootNode, null);
    }

    /// <summary>
/// Parses the specified track using ISO 9660.
/// </summary>
/// <param name="track">The track to parse.</param>
/// <param name="rootNode">The root FsNode to populate.</param>
/// <returns>true if parsing succeeded.</returns>
    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        return _isoParser.Parse(rootNode, track);
    }
}

/// <summary>
/// Parses Sega Genesis CD / Mega CD disc images using ISO 9660.
/// </summary>
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

/// <summary>
/// Parses Sega Saturn disc images using ISO 9660.
/// </summary>
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

/// <summary>
/// Parses SNK NeoGeo CD disc images using ISO 9660.
/// </summary>
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

/// <summary>
/// Parses Commodore Amiga CD32 disc images using ISO 9660.
/// </summary>
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

/// <summary>
/// Parses Commodore Amiga CD disc images using ISO 9660.
/// </summary>
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

/// <summary>
/// Parses Sharp X68000 disc images using ISO 9660, falling back to UDF if ISO 9660 fails.
/// </summary>
public class X68000Parser : IConsoleParser
{
    private readonly SectorReader _reader;

    public X68000Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public bool ForceMode { get; set; }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.X68000;
    }

    public string GetConsoleName()
    {
        return "X68000";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var isoParser = new Iso9660Parser(_reader);
        if (isoParser.Parse(rootNode, track))
            return true;

        var udfParser = new UdfParser(_reader);
        return udfParser.Parse(rootNode, track);
    }

    private TrackInfo FindDataTrack()
    {
        foreach (var t in _reader.Tracks)
            if (t.IsDataTrack) return t;

        return _reader.Tracks.Count > 0 ? _reader.Tracks[0] : new TrackInfo();
    }
}

/// <summary>
/// Parses NEC PC-98 disc images using ISO 9660.
/// </summary>
public class Pc98Parser : Iso9660Wrapper
{
    public Pc98Parser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.Pc98;
    }

    public override string GetConsoleName()
    {
        return "PC-98";
    }
}

/// <summary>
/// Parses Fujitsu FM Towns disc images using ISO 9660.
/// </summary>
public class FmTownsParser : Iso9660Wrapper
{
    public FmTownsParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.FmTowns;
    }

    public override string GetConsoleName()
    {
        return "FM Towns";
    }
}

/// <summary>
/// Parses Sega Pico disc images using ISO 9660.
/// </summary>
public class PicoParser : Iso9660Wrapper
{
    public PicoParser(SectorReader reader) : base(reader)
    {
    }

    public override ConsoleType GetConsoleType()
    {
        return ConsoleType.Pico;
    }

    public override string GetConsoleName()
    {
        return "Sega Pico";
    }
}

/// <summary>
/// Parses Apple Bandai Pippin disc images using HFS (Macintosh Hierarchical File System).
/// </summary>
public class PippinParser : IConsoleParser
{
    private readonly SectorReader _reader;
    private HfsParser? _hfsParser;
    public bool ForceMode { get; set; }

    public PippinParser(SectorReader reader)
    {
        _reader = reader;
    }

    public ConsoleType GetConsoleType()
    {
        return ConsoleType.Pippin;
    }

    public string GetConsoleName()
    {
        return "Pippin";
    }

    public bool Parse(FsNode rootNode)
    {
        return ParseTrack(rootNode, FindDataTrack());
    }

    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        _hfsParser ??= new HfsParser(_reader);

        if (_hfsParser.Parse(rootNode, track))
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

public abstract class Iso9660Wrapper : IConsoleParser
{
    /// <summary>
/// The sector reader used by this parser.
/// </summary>
    protected SectorReader Reader { get; }
    public bool ForceMode { get; set; }

    /// <summary>
/// Initializes a new instance of the Iso9660Wrapper class.
/// </summary>
/// <param name="reader">The SectorReader to read sectors from.</param>
    protected Iso9660Wrapper(SectorReader reader)
    {
        Reader = reader;
    }

    /// <summary>
/// Returns the ConsoleType that this parser handles.
/// </summary>
/// <returns>The console type.</returns>
    public abstract ConsoleType GetConsoleType();
    /// <summary>
/// Returns the human-readable console name.
/// </summary>
/// <returns>The display name.</returns>
    public abstract string GetConsoleName();

    /// <summary>
/// Parses the first data track found in the reader using ISO 9660.
/// </summary>
/// <param name="rootNode">The root FsNode to populate.</param>
/// <returns>true if parsing succeeded.</returns>
    public bool Parse(FsNode rootNode)
    {
        var track = FindDataTrack();
        if (track == null)
            return false;

        return ParseTrack(rootNode, track);
    }

    /// <summary>
/// Parses the specified track using ISO 9660.
/// </summary>
/// <param name="track">The track to parse.</param>
/// <param name="rootNode">The root FsNode to populate.</param>
/// <returns>true if parsing succeeded.</returns>
    public bool ParseTrack(FsNode rootNode, TrackInfo track)
    {
        var parser = new Iso9660Parser(Reader);
        return parser.Parse(rootNode, track);
    }

    /// <summary>
/// Finds the first data track in the reader.
/// </summary>
/// <returns>The first data TrackInfo, or null.</returns>
    protected TrackInfo? FindDataTrack()
    {
        foreach (var t in Reader.Tracks)
            if (t.IsDataTrack) return t;

        return Reader.Tracks.FirstOrDefault();
    }
}
