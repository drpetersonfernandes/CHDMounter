using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class ThreeDoIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public ThreeDoIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> ThreeDoChdPaths =>
    [
        @"G:\MAME\MAME Software List CHDs\3do\cpquazar\captain quazar (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\espnfit\espn fitness pros - step aerobics (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\virtuoso\virtuoso (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\vpreika\virtual puppet reika (japan).chd",
        @"G:\MAME\MAME Software List CHDs\3do\cowcasn\cowboy casino (usa).chd",
        @"I:\Panasonic 3DO\20th Century Video Almanac (USA).chd",
        @"I:\Panasonic 3DO\3D Atlas (USA).chd",
        @"I:\Panasonic 3DO\Alone in the Dark (USA).chd",
        @"I:\Panasonic 3DO\Battle Chess (USA).chd"
    ];

    public static TheoryData<string> AllThreeDoChdPaths =>
    [
        @"G:\MAME\MAME Software List CHDs\3do\cpquazar\captain quazar (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\espnfit\espn fitness pros - step aerobics (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\virtuoso\virtuoso (usa).chd",
        @"G:\MAME\MAME Software List CHDs\3do\vpreika\virtual puppet reika (japan).chd",
        @"G:\MAME\MAME Software List CHDs\3do\cowcasn\cowboy casino (usa).chd",
        @"I:\Panasonic 3DO\20th Century Video Almanac (USA).chd",
        @"I:\Panasonic 3DO\3D Atlas (Europe).chd",
        @"I:\Panasonic 3DO\3D Atlas (USA).chd",
        @"I:\Panasonic 3DO\3DO de Shiru Miru Asobu - Nakajima Miyuki (Japan).chd",
        @"I:\Panasonic 3DO\3DO Game Guru (USA, Europe).chd",
        @"I:\Panasonic 3DO\Adventures Of Captain J, The (USA) (Unl).chd",
        @"I:\Panasonic 3DO\AI Shougi (Japan).chd",
        @"I:\Panasonic 3DO\Alone in the Dark (Europe) (En,Fr) (NTSC Version).chd",
        @"I:\Panasonic 3DO\Alone in the Dark (Europe) (En,Fr).chd",
        @"I:\Panasonic 3DO\Alone in the Dark (Japan).chd",
        @"I:\Panasonic 3DO\Alone in the Dark (USA).chd",
        @"I:\Panasonic 3DO\Alone in the Dark 2 (Europe).chd",
        @"I:\Panasonic 3DO\Alone in the Dark 2 (Japan).chd",
        @"I:\Panasonic 3DO\Alone in the Dark 2 (USA).chd",
        @"I:\Panasonic 3DO\Another World (Europe).chd",
        @"I:\Panasonic 3DO\AutoBahn Tokio (Japan).chd",
        @"I:\Panasonic 3DO\Battle Chess (Europe).chd",
        @"I:\Panasonic 3DO\Battle Chess (Japan).chd",
        @"I:\Panasonic 3DO\Battle Chess (USA).chd",
        @"I:\Panasonic 3DO\BattleSport (USA).chd"
    ];

    [Theory]
    [MemberData(nameof(ThreeDoChdPaths))]
    public void ThreeDoParserParsesThreeDoDisc(string chdPath)
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
            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count}");

            var root = new FsNode();
            var parser = new ThreeDoParser(reader);

            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
            var ok = parser.Parse(root, track);
            _output.WriteLine($"ThreeDoParser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "ThreeDoParser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 2, $"Suspiciously few files parsed: {files}");

            foreach (var c in root.Children.OrderByDescending(static n => n.Size).Take(15))
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ThreeDoChdPaths))]
    public void ThreeDoConsoleParserParsesThreeDoDisc(string chdPath)
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
            _output.WriteLine($"UnitBytes={chd.UnitBytes} Tracks={reader.Tracks.Count}");

            var root = new FsNode();
            var parser = new ThreeDoConsoleParser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"ThreeDoConsoleParser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "ThreeDoConsoleParser could not parse the disc");

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

            Assert.True(files > 2, $"Suspiciously few files parsed: {files}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ThreeDoChdPaths))]
    public void ChdContainerMountAndParseThreeDoDisc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.ThreeDo), "MountAndParse failed");

            var all = CollectEntries(container, "\\").ToList();
            var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
            _output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

            Assert.True(fileEntries.Count > 2, $"Suspiciously few files: {fileEntries.Count}");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Contains('\0')).ToList();
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
    [MemberData(nameof(AllThreeDoChdPaths))]
    public void BulkParseAllThreeDoDiscs(string chdPath)
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
            var parser = new ThreeDoParser(reader);
            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

            var ok = parser.Parse(root, track);
            var fileName = Path.GetFileName(chdPath);

            int files = 0, dirs = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref maxSize);

            _output.WriteLine($"[{(ok ? "OK" : "FAIL")}] {fileName}  UnitBytes={chd.UnitBytes}  Tracks={reader.Tracks.Count}  Files={files}  Dirs={dirs}  MaxFile={maxSize:N0}");

            if (ok)
            {
                Assert.True(files > 2, $"Suspiciously few files parsed: {files}");
            }
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
                    maxSize = c.Size;
            }
        }
    }

    public static TheoryData<string> ThreeDoM2ChdPaths =>
    [
        @"G:\MAME\MAME Software List CHDs\3do_m2\olds\oldsmobile (disc 2).chd",
        @"G:\MAME\MAME Software List CHDs\3do_m2\imsarcng\imsa racing.chd",
        @"G:\MAME\MAME Software List CHDs\3do_m2\shootr2d\shootr2d.chd"
    ];

    private static void WalkTest(FsNode root, ITestOutputHelper output, out int files, out int dirs, out ulong maxSize)
    {
        files = 0;
        dirs = 0;
        maxSize = 0;
        Walk(root, ref files, ref dirs, ref maxSize);
        output.WriteLine($"  FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");
    }

    [Theory]
    [MemberData(nameof(ThreeDoM2ChdPaths))]
    public void M2DiscPaserDiagnostic(string chdPath)
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
            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
            var fileName = Path.GetFileName(chdPath);

            _output.WriteLine($"--- {fileName} (UnitBytes={chd.UnitBytes}, Tracks={reader.Tracks.Count}) ---");

            var ok3do = TryThreeDo(reader, track, out var f3, out var d3, out var m3);
            _output.WriteLine($"  ThreeDoParser: {(ok3do ? $"OK ({f3} files, {d3} dirs, max={m3:N0})" : "FAIL")}");

            reader = new SectorReader(chd, chd.UnitBytes);
            track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
            var okIso = TryIso9660(reader, track, out var fi, out var di);
            _output.WriteLine($"  Iso9660Parser: {(okIso ? $"OK ({fi} files, {di} dirs)" : "FAIL")}");

            var okThreeDoCt = TryContainerMount(chdPath, ConsoleType.ThreeDo, out var c3f, out var c3d);
            _output.WriteLine($"  Container ThreeDo: {(okThreeDoCt ? $"OK ({c3f} files, {c3d} dirs)" : "FAIL")}");

            var okIsoCt = TryContainerMount(chdPath, ConsoleType.GenericIso9660, out var cif, out var cid);
            _output.WriteLine($"  Container ISO9660: {(okIsoCt ? $"OK ({cif} files, {cid} dirs)" : "FAIL")}");
        }
        finally
        {
            chd.Dispose();
        }
    }

    private bool TryThreeDo(SectorReader reader, TrackInfo track, out int files, out int dirs, out ulong maxSize)
    {
        files = 0;
        dirs = 0;
        maxSize = 0;
        var root = new FsNode();
        var parser = new ThreeDoParser(reader);
        var ok = parser.Parse(root, track);
        if (ok) Walk(root, ref files, ref dirs, ref maxSize);
        return ok;
    }

    private bool TryIso9660(SectorReader reader, TrackInfo track, out int files, out int dirs)
    {
        files = 0;
        dirs = 0;
        ulong maxSize = 0;
        var root = new FsNode();
        var parser = new Iso9660Parser(reader);
        var ok = parser.Parse(root, track);
        if (ok) Walk(root, ref files, ref dirs, ref maxSize);
        return ok;
    }

    private bool TryContainerMount(string chdPath, ConsoleType consoleType, out int files, out int dirs)
    {
        files = 0;
        dirs = 0;
        try
        {
            var container = new ChdContainer(chdPath);
            try
            {
                if (!container.MountAndParse(consoleType))
                    return false;

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                files = fileEntries.Count;
                dirs = all.Count - fileEntries.Count;
                return fileEntries.Count > 2;
            }
            finally
            {
                container.Dispose();
            }
        }
        catch
        {
            return false;
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
