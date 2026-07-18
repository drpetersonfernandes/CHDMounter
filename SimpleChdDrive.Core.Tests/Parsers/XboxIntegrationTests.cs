using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class XboxIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public XboxIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> XboxChdPaths =>
    [
        @"J:\Microsoft Xbox\007 - Agent Under Fire (USA).chd",
        @"J:\Microsoft Xbox\007 - Everything or Nothing (USA).chd",
        @"J:\Microsoft Xbox\007 - From Russia with Love (USA).chd",
        @"J:\Microsoft Xbox\007 - Nightfire (USA).chd",
        @"J:\Microsoft Xbox\187 - Ride or Die (USA, Europe) (En,Fr,De,Es,It).chd"
    ];

    public static TheoryData<string> Xbox360ChdPaths =>
    [
        @"X:\Microsoft Xbox 360\007 - Blood Stone (USA, Europe) (En,Fr,De).chd",
        @"X:\Microsoft Xbox 360\007 - Quantum of Solace (USA, Europe) (En,Fr).chd",
        @"X:\Microsoft Xbox 360\007 Legends (USA, Europe) (En,Fr,De).chd",
        @"X:\Microsoft Xbox 360\007 Legends (USA) (En,Fr,De).chd",
        @"X:\Microsoft Xbox 360\2010 FIFA World Cup South Africa (USA, Asia) (En,Fr,Es).chd"
    ];

    public static TheoryData<string> DreamcastChdPaths =>
    [
        @"X:\Sega Dreamcast\09 Chairs (Japan).chd",
        @"X:\Sega Dreamcast\18 Wheeler - American Pro Trucker (Europe) (En,Fr,De,Es).chd",
        @"X:\Sega Dreamcast\18 Wheeler - American Pro Trucker (Japan).chd",
        @"X:\Sega Dreamcast\18 Wheeler - American Pro Trucker (USA).chd",
        @"X:\Sega Dreamcast\21 - Two One (Japan).chd"
    ];

    [Theory]
    [MemberData(nameof(XboxChdPaths))]
    public void XdvdfsParserParsesXboxDisc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            var unitBytes = chd.UnitBytes;
            var reader = new SectorReader(chd, unitBytes);
            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track?.TrackType ?? "N/A"}");

            var root = new FsNode();
            var parser = new XdvdfsParser(reader);
            parser.SetTrack(track!);
            var ok = parser.Parse(root);
            _output.WriteLine($"XdvdfsParser: {(ok ? "OK" : "FAILED")}");

            if (!ok)
            {
                _output.WriteLine("Trying Iso9660Parser fallback...");
                var isoParser = new Iso9660Parser(reader);
                ok = isoParser.Parse(root, track);
                _output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");
            }

            Assert.True(ok, "Neither XdvdfsParser nor Iso9660Parser could parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
            foreach (var c in topTwenty)
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    private static void Walk(FsNode node, ref int files, ref int dirs, ref ulong maxSize)
    {
        foreach (var c in node.Children)
        {
            if (c.IsDirectory)
            {
                dirs++;
                Walk(c, ref files, ref dirs, ref maxSize);
            }
            else
            {
                files++;
                if (c.Size > maxSize)
                {
                    maxSize = c.Size;
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Xbox360ChdPaths))]
    public void XdvdfsAndUdfParserParsesXbox360Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            var unitBytes = chd.UnitBytes;
            var reader = new SectorReader(chd, unitBytes);
            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track?.TrackType ?? "N/A"}");

            var root = new FsNode();
            var parser = new XdvdfsParser(reader);
            parser.SetTrack(track!);
            var ok = parser.Parse(root);
            _output.WriteLine($"XdvdfsParser: {(ok ? "OK" : "FAILED")}");

            if (!ok)
            {
                _output.WriteLine("Trying UdfParser fallback...");
                var udfParser = new UdfParser(reader);
                ok = udfParser.Parse(root, track);
                _output.WriteLine($"UdfParser: {(ok ? "OK" : "FAILED")}");
            }

            if (!ok)
            {
                _output.WriteLine("Trying Iso9660Parser fallback...");
                var isoParser = new Iso9660Parser(reader);
                ok = isoParser.Parse(root, track);
                _output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");
            }

            Assert.True(ok, "No parser could parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);

            var totalSize = (ulong)0;
            CountTotal(root, ref totalSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, total={totalSize:N0} bytes, largest file {maxSize:N0} bytes");

            var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
            foreach (var c in topTwenty)
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");

            var xexFiles = FindNodes(root).Count(n => !n.IsDirectory && n.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"XEX files found: {xexFiles}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(DreamcastChdPaths))]
    public void Iso9660ParserParsesDreamcastDisc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            var unitBytes = chd.UnitBytes;
            var reader = new SectorReader(chd, unitBytes);
            var track = reader.Tracks.LastOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track?.TrackType ?? "N/A"}");
            foreach (var t in reader.Tracks)
                _output.WriteLine($"  idx={t.Index} LBA={t.StartLba} frames={t.Frames} type={t.TrackType} data={t.IsDataTrack}");

            var root = new FsNode();
            var parser = new Iso9660Parser(reader);

            var offsets = new[] { -45000, -45150, -150, 0, 45000, 45150 };
            var ok = false;
            var usedOffset = 0;
            foreach (var offset in offsets)
            {
                parser.SetLbaOffset(offset);
                ok = parser.Parse(root, track);
                if (ok)
                {
                    usedOffset = offset;
                    break;
                }
            }

            if (!ok)
            {
                ok = parser.Parse(root, track);
                usedOffset = 0;
            }

            _output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}  LbaOffset={usedOffset}");
            _output.WriteLine($"Root LBA={root.Lba} Size={root.Size}");

            Assert.True(ok, "Iso9660Parser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);

            var allNodes = FindNodes(root);
            var rockRidgeNodes = allNodes.Count(n => n.UnixMode.HasValue);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes, RR entries={rockRidgeNodes}");

            var topFifteen = root.Children.OrderByDescending(static n => n.Size).Take(15);
            foreach (var c in topFifteen)
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

            Assert.True(files >= 5, $"Suspiciously few files parsed: {files}");

            var executables = allNodes.Count(n => !n.IsDirectory && n.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"BIN files: {executables}");

            if (rockRidgeNodes > 0)
            {
                var sample = allNodes.First(n => n.UnixMode.HasValue);
                _output.WriteLine($"Rock Ridge sample: {sample.Name} mode=0x{sample.UnixMode:X8} uid={sample.Uid} gid={sample.Gid}");
            }
        }
        finally
        {
            chd.Dispose();
        }
    }

    private static List<FsNode> FindNodes(FsNode node)
    {
        var list = new List<FsNode> { node };
        foreach (var c in node.Children)
            list.AddRange(FindNodes(c));
        return list;
    }

    [Theory]
    [MemberData(nameof(DreamcastChdPaths))]
    public void DiagnoseSectorReaderForGdrom(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            var unitBytes = chd.UnitBytes;
            var hunkBytes = chd.HunkBytes;
            var sectorsPerHunk = hunkBytes / unitBytes;
            var totalFrames = (uint)(chd.TotalBytes / unitBytes);

            _output.WriteLine($"UnitBytes={unitBytes} HunkBytes={hunkBytes} sectorsPerHunk={sectorsPerHunk} totalFrames={totalFrames}");

            var reader = new SectorReader(chd, unitBytes);
            _output.WriteLine($"Tracks: {reader.Tracks.Count}");
            foreach (var t in reader.Tracks)
                _output.WriteLine($"  idx={t.Index} LBA={t.StartLba} frames={t.Frames} ChdOff={t.ChdOffset} data={t.IsDataTrack} type={t.TrackType}");

            // Find the last data track (HD area)
            TrackInfo? hdTrack = null;
            for (var i = reader.Tracks.Count - 1; i >= 0; i--)
            {
                if (reader.Tracks[i].IsDataTrack)
                {
                    hdTrack = reader.Tracks[i];
                    break;
                }
            }

            if (hdTrack == null)
            {
                _output.WriteLine("No data track found, skipping");
                return;
            }

            _output.WriteLine($"HD track: idx={hdTrack.Index} LBA={hdTrack.StartLba} frames={hdTrack.Frames} ChdOff={hdTrack.ChdOffset}");

            var buf = new byte[2048];

            // Probe: read sector 0 of the HD track (should be IP.BIN)
            var ipBinLba = hdTrack.StartLba;
            var ipBinOk = reader.ReadSector(ipBinLba, buf);
            _output.WriteLine($"IP.BIN at LBA={ipBinLba}: {(ipBinOk ? "OK" : "FAIL")}");
            if (ipBinOk)
            {
                var id = System.Text.Encoding.ASCII.GetString(buf, 0, 16).TrimEnd('\0');
                _output.WriteLine($"  [0-15]: '{id}'");
            }

            // Probe: read sector 16 of HD track (PVD)
            var pvdLba = hdTrack.StartLba + 16;
            var pvdOk = reader.ReadSector(pvdLba, buf);
            _output.WriteLine($"PVD at LBA={pvdLba}: {(pvdOk ? "OK" : "FAIL")}");
            if (pvdOk)
            {
                var type = buf[0];
                var magic = System.Text.Encoding.ASCII.GetString(buf, 1, 5);
                var rlba = BitConverter.ToUInt32(buf, 158);
                _output.WriteLine($"  type={type} magic='{magic}' rootLBA={rlba}");
            }

            // Probe the key root directory LBA candidates
            var rootLbaFromPvd = 0u;
            if (reader.ReadSector(hdTrack.StartLba + 16, buf))
            {
                rootLbaFromPvd = BitConverter.ToUInt32(buf, 158);
            }

            _output.WriteLine($"PVD rootLBA={rootLbaFromPvd}");

            // Candidate A: session-relative (pvd rootLBA is LBA within session, root at StartLba + rootLBA)
            var candA = hdTrack.StartLba + rootLbaFromPvd;
            // Candidate B: absolute (pvd rootLBA is disc-absolute LBA)
            var candB = rootLbaFromPvd;
            // Candidate C: simple (root at session start + 20, pvd rootLBA might include session offset 45000)
            var rootLbaSimple = rootLbaFromPvd > 45000 ? rootLbaFromPvd - 45000 : rootLbaFromPvd;
            var candC = hdTrack.StartLba + rootLbaSimple;

            foreach (var (label, lba) in new[] { ("A:start+root", candA), ("B:absolute", candB), ("C:start+root-norm", candC) })
            {
                var ok = reader.ReadSector(lba, buf);
                var b0 = buf[0];
                var magic = ok ? System.Text.Encoding.ASCII.GetString(buf, 1, 5) : "";
                _output.WriteLine($"  {label}: LBA={lba} ok={ok} byte0={b0:X2} magic='{magic}'");
                if (ok && b0 >= 34 && (b0 & 1) == 0)
                {
                    const int nameLenPos = 32;
                    {
                        var nameLen = buf[nameLenPos];
                        var name = System.Text.Encoding.ASCII.GetString(buf, 33, Math.Min(nameLen, b0 - 33)).Trim('\0');
                        _output.WriteLine($"    Looks like a dir record! nameLen={nameLen} name='{name}'");
                    }
                }
            }
        }
        finally
        {
            chd.Dispose();
        }
    }

    private static void CountTotal(FsNode node, ref ulong total)
    {
        foreach (var c in node.Children)
        {
            if (!c.IsDirectory)
            {
                total += c.Size;
            }

            CountTotal(c, ref total);
        }
    }
}
