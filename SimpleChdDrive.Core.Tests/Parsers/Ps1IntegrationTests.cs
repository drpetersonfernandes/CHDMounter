using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class Ps1IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public Ps1IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> Ps1ChdPaths
    {
        get
        {
            var data = new TheoryData<string>();
            string[] dirs = [@"G:\MAME\MAME Software List CHDs\psx", @"X:\Sony PlayStation 1"];

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                    continue;

                foreach (var chd in Directory.EnumerateFiles(dir, "*.chd", SearchOption.AllDirectories))
                    data.Add(chd);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Ps1ChdPaths))]
    public void Iso9660ParserParsesPs1Disc(string chdPath)
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
    [MemberData(nameof(Ps1ChdPaths))]
    public void PlayStation1ParserParsesPs1Disc(string chdPath)
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
            var parser = new PlayStation1Parser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"PlayStation1Parser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "PlayStation1Parser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");

            var hasSystemCnf = root.Children.Any(static n => n.Name == "SYSTEM.CNF");
            var hasExe = root.Children.Any(static n => n.Name.EndsWith(".EXE", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"SYSTEM.CNF: {(hasSystemCnf ? "YES" : "NO")}  .EXE file: {(hasExe ? "YES" : "NO")}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Ps1ChdPaths))]
    public void ChdContainerMountAndParsePs1Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.Ps1), "MountAndParse failed");

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
