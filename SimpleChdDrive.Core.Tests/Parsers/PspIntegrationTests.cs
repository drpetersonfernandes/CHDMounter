using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class PspIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PspIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> PspChdPaths =>
    [
        @"X:\Sony PSP\007 - From Russia with Love (USA).chd",
        @"X:\Sony PSP\300 - March to Glory (USA).chd",
        @"X:\Sony PSP\3rd Birthday, The (USA).chd",
        @"X:\Sony PSP\7 Wonders of the Ancient World (USA).chd",
        @"X:\Sony PSP\50 Cent - Bulletproof - G-Unit Edition (USA).chd"
    ];

    [Theory]
    [MemberData(nameof(PspChdPaths))]
    public void Iso9660ParserParsesPspDisc(string chdPath)
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

            Assert.True(files > 5, $"Suspiciously few files parsed: {files}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(PspChdPaths))]
    public void PspParserParsesPspDisc(string chdPath)
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
            var parser = new PspParser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"PspParser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "PspParser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 5, $"Suspiciously few files parsed: {files}");

            var hasPspGame = root.Children.Any(static n => n is { Name: "PSP_GAME", IsDirectory: true });
            var hasUmdDataBin = root.Children.Any(static n => n.Name == "UMD_DATA.BIN");
            _output.WriteLine($"PSP_GAME: {(hasPspGame ? "YES" : "NO")}  UMD_DATA.BIN: {(hasUmdDataBin ? "YES" : "NO")}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(PspChdPaths))]
    public void ChdContainerMountAndParsePspDisc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.Psp), "MountAndParse failed");

            foreach (var e in container.ListDirectory("\\"))
                _output.WriteLine($"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

            var all = CollectEntries(container, "\\").ToList();
            var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
            _output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

            Assert.True(fileEntries.Count > 5, $"Suspiciously few files: {fileEntries.Count}");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
            foreach (var bad in badNames)
                _output.WriteLine($"BAD NAME: {bad.FullPath}");
            Assert.Empty(badNames);

            var umdData = container.FindFile(@"\UMD_DATA.BIN");
            if (umdData != null)
            {
                var buf = new byte[2048];
                var bytesRead = container.ReadFile(umdData, 0, buf, 0, buf.Length);
                var title = Encoding.ASCII.GetString(buf, 0, Math.Min(bytesRead, 128)).TrimEnd('\0');
                _output.WriteLine($"UMD_DATA.BIN ({bytesRead} bytes): '{title}'");
            }
            else
            {
                _output.WriteLine("UMD_DATA.BIN: NOT FOUND");
            }

            var paramSfo = container.FindFile(@"\PSP_GAME\PARAM.SFO");
            if (paramSfo != null)
                _output.WriteLine($"PSP_GAME\\PARAM.SFO: FOUND ({paramSfo.Size:N0} bytes)");
            else
                _output.WriteLine("PSP_GAME\\PARAM.SFO: NOT FOUND");
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
                foreach (var sub in CollectEntries(container, e.FullPath))
                    yield return sub;
        }
    }
}
