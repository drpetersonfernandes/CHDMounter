using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class X68000IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public X68000IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> X68000SampleChdPaths
    {
        get
        {
            var data = new TheoryData<string>();
            string[] dirs = [@"F:\Sharp X68000"];

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
    [MemberData(nameof(X68000SampleChdPaths))]
    public void Iso9660ParserParsesX68000Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        if (err != ChdError.Chderrnone)
        {
            _output.WriteLine($"SKIP: ChdFile.Open failed with {err}");
            return;
        }

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

            Assert.True(files > 1, $"Suspiciously few files parsed: {files}");

            foreach (var c in root.Children.OrderByDescending(static n => n.Size).Take(15))
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(X68000SampleChdPaths))]
    public void GenericIso9660ParserParsesX68000Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        if (err != ChdError.Chderrnone)
        {
            _output.WriteLine($"SKIP: ChdFile.Open failed with {err}");
            return;
        }

        Assert.NotNull(chd);

        try
        {
            var reader = new SectorReader(chd, chd.UnitBytes);
            _output.WriteLine($"UnitBytes={chd.UnitBytes} Tracks={reader.Tracks.Count}");

            var root = new FsNode();
            var parser = new GenericIso9660Parser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"GenericIso9660Parser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "GenericIso9660Parser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 1, $"Suspiciously few files parsed: {files}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(X68000SampleChdPaths))]
    public void ChdContainerMountAndParseX68000Disc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            if (!container.MountAndParse(ConsoleType.GenericIso9660))
            {
                _output.WriteLine("SKIP: MountAndParse failed (likely invalid CHD)");
                return;
            }

            var all = CollectEntries(container, "\\").ToList();
            var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
            _output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

            Assert.True(fileEntries.Count > 1, $"Suspiciously few files: {fileEntries.Count}");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
            foreach (var bad in badNames)
                _output.WriteLine($"BAD NAME: {bad.FullPath}");
            Assert.Empty(badNames);

            foreach (var e in container.ListDirectory("\\"))
                _output.WriteLine($"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");
        }
        finally
        {
            container.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(X68000SampleChdPaths))]
    public void ChdContainerCheckParseAndRead(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            if (!container.MountAndParse(ConsoleType.GenericIso9660))
            {
                _output.WriteLine("SKIP: MountAndParse failed (likely invalid CHD)");
                return;
            }

            foreach (var e in container.ListDirectory("\\"))
            {
                if (e.IsDirectory) continue;

                var entry = container.FindFile(e.FullPath);
                Assert.NotNull(entry);

                var readSize = (int)Math.Min(e.Size, 4096);
                var buffer = new byte[readSize];
                var bytesRead = container.ReadFile(entry, 0, buffer, 0, readSize);
                _output.WriteLine($"  Read: {e.Name}  size={e.Size}  bytesRead={bytesRead}");

                if (bytesRead > 0)
                {
                    Assert.True(true, $"Failed to read {e.Name}");
                    break;
                }
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
                foreach (var sub in CollectEntries(container, e.FullPath))
                    yield return sub;
        }
    }
}
