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

    public static TheoryData<string> XboxChdPaths
    {
        get
        {
            var data = new TheoryData<string>();
            const string dir = @"J:\Microsoft Xbox";

            if (Directory.Exists(dir))
            {
                foreach (var chd in Directory.EnumerateFiles(dir, "*.chd", SearchOption.AllDirectories))
                    data.Add(chd);
            }

            return data;
        }
    }

    public static TheoryData<string> Xbox360ChdPaths
    {
        get
        {
            var data = new TheoryData<string>();
            const string dir = @"X:\Microsoft Xbox 360";

            if (Directory.Exists(dir))
            {
                foreach (var chd in Directory.EnumerateFiles(dir, "*.chd", SearchOption.AllDirectories))
                    data.Add(chd);
            }

            return data;
        }
    }

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
            Assert.NotNull(track);

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track.TrackType}");

            var root = new FsNode();
            var parser = new XdvdfsParser(reader);
            parser.SetTrack(track);
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
            Assert.NotNull(track);

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track.TrackType}");

            var root = new FsNode();
            var parser = new XdvdfsParser(reader);
            parser.SetTrack(track);
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

    private static List<FsNode> FindNodes(FsNode node)
    {
        var list = new List<FsNode> { node };
        foreach (var c in node.Children)
            list.AddRange(FindNodes(c));
        return list;
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
