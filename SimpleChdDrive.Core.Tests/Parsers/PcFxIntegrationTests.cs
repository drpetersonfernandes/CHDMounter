using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class PcFxIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PcFxIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> PcFxSampleChdPaths =>
    [
        // MAME Software List CHDs (nested in subdirectories)
        @"G:\MAME\MAME Software List CHDs\pcfx\batlheat\battle heat (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\dknight4\dragon knight 4 (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\farland\farland story fx (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\langriss\langrisser fx, der (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\megamip2\megami paradise ii (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\amateur\amateur teikyou cd-rom (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\angeliq\fushigi no kuni no angelique (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\cancandx\can can bunny extra dx (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\carrot\pia carrot e youkoso weve been waiting for you (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\doukyuu2\doukyuusei 2 (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\firewomn\fire woman matoigumi (japan).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\nnyuu\n-nyuu - pc-fxga game ga asoberu tsukureru hon (japan) (disc 1) (n-nyuu).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\samegame\same game fx (japan) (nec pc-fxga).chd",
        @"G:\MAME\MAME Software List CHDs\pcfx\sotsugy2\sotsugyou ii fx - neo generation (japan).chd",

        // Flat directory CHDs
        @"G:\NEC PC-FX\Aa Megami-sama (Japan) (Disc 1).chd",
        @"G:\NEC PC-FX\AnimeFreak FX Vol. 1 (Japan).chd",
        @"G:\NEC PC-FX\Battle Heat (Japan).chd",
        @"G:\NEC PC-FX\Chip-chan Kick! (Japan).chd",
        @"G:\NEC PC-FX\Dragon Knight 4 (Japan).chd",
        @"G:\NEC PC-FX\Langrisser FX, Der (Japan).chd",
        @"G:\NEC PC-FX\Pia Carrot e Youkoso!! We've Been Waiting for You (Japan).chd",
        @"G:\NEC PC-FX\Sotsugyou II FX - Neo Generation (Japan) (SABS, SACS).chd",
        @"G:\NEC PC-FX\Super PC Engine Fan Deluxe - Special CD-ROM Vol. 1 (Japan).chd",
        @"G:\NEC PC-FX\Team Innocent - The Point of No Return - G.C.P.O.SS (Japan).chd"
    ];

    [Theory]
    [MemberData(nameof(PcFxSampleChdPaths))]
    public void Iso9660ParserParsesPcFxDisc(string chdPath)
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
    [MemberData(nameof(PcFxSampleChdPaths))]
    public void PcFxParserParsesDisc(string chdPath)
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
            var parser = new PcFxParser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"PcFxParser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "PcFxParser could not parse the disc");

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
    [MemberData(nameof(PcFxSampleChdPaths))]
    public void ChdContainerMountAndParsePcFxDisc(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.PcFx), "MountAndParse failed");

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
    [MemberData(nameof(PcFxSampleChdPaths))]
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
            Assert.True(container.MountAndParse(ConsoleType.PcFx), "MountAndParse failed");

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
