using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class Ps2IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public Ps2IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> Ps2ChdPaths =>
    [
        @"X:\Sony PlayStation 2\0 Story (Japan) (Disc 1).chd",
        @"X:\Sony PlayStation 2\0 Story (Japan) (Disc 2).chd",
        @"X:\Sony PlayStation 2\0 Story (Japan) (Taikenban).chd",
        @"X:\Sony PlayStation 2\007 - Agent Under Fire (USA).chd",
        @"X:\Sony PlayStation 2\007 - Everything or Nothing (Japan).chd"
    ];

    [Theory]
    [MemberData(nameof(Ps2ChdPaths))]
    public void Iso9660ParserParsesPs2Disc(string chdPath)
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
            var parser = new Iso9660Parser(reader);

            var ok = parser.Parse(root, track);
            _output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "Iso9660Parser could not parse the disc");

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

    [Theory]
    [MemberData(nameof(Ps2ChdPaths))]
    public void PlayStation2ParserParsesPs2Disc(string chdPath)
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
            var reader = new SectorReader(chd, chd.UnitBytes);
            var root = new FsNode();
            var parser = new PlayStation2Parser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"PlayStation2Parser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "PlayStation2Parser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");

            var hasSystemCnf = root.Children.Any(static n => n.Name == "SYSTEM.CNF");
            var hasIop = root.Children.Any(static n => n.Name.StartsWith("IOP", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"SYSTEM.CNF: {(hasSystemCnf ? "YES" : "NO")}  IOP modules: {(hasIop ? "YES" : "NO")}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Ps2ChdPaths))]
    public void ChdContainerMountAndParsePs2Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.Ps2), "MountAndParse failed");

            foreach (var e in container.ListDirectory("\\"))
                _output.WriteLine($"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

            var all = CollectEntries(container, "\\").ToList();
            var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
            _output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

            Assert.True(fileEntries.Count > 10, $"Suspiciously few files: {fileEntries.Count}");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
            foreach (var bad in badNames)
                _output.WriteLine($"BAD NAME: {bad.FullPath}");
            Assert.Empty(badNames);

            var systemCnf = container.FindFile(@"\SYSTEM.CNF");
            if (systemCnf != null)
            {
                var buf = new byte[256];
                var bytesRead = container.ReadFile(systemCnf, 0, buf, 0, buf.Length);
                var text = Encoding.ASCII.GetString(buf, 0, bytesRead);
                _output.WriteLine($"SYSTEM.CNF ({bytesRead} bytes): {text[..Math.Min(text.Length, 200)].Replace("\r", "").Replace("\n", " / ")}");
            }
            else
            {
                _output.WriteLine("SYSTEM.CNF: NOT FOUND");
            }
        }
        finally
        {
            container.Dispose();
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

    private static IEnumerable<FileEntry> CollectEntries(ChdContainer container, string path)
    {
        foreach (var e in container.ListDirectory(path))
        {
            yield return e;

            if (e.IsDirectory)
            {
                foreach (var sub in CollectEntries(container, e.FullPath))
                    yield return sub;
            }
        }
    }
}
