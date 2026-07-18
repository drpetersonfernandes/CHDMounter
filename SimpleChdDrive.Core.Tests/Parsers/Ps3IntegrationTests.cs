using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class Ps3IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public Ps3IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> ChdPaths =>
    [
        @"D:\Samples\PSX3\007 - Blood Stone (USA) (En,Fr).chd",
        @"D:\Samples\PSX3\Amazing Spider-Man, The (USA) (En,Fr,Es).chd"
    ];

    [Theory]
    [MemberData(nameof(ChdPaths))]
    public void UdfParserParsesPs3Disc(string chdPath)
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

            var root = new FsNode();
            var udfOk = new UdfParser(reader).Parse(root, track);
            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} UdfParser={(udfOk ? "OK" : "FAILED")}");
            Assert.True(udfOk, "UdfParser.Parse failed (PS3 would fall back to ISO9660)");

            int files = 0, dirs = 0, multiExtent = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref multiExtent, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, {multiExtent} multi-extent files, largest file {maxSize:N0} bytes");

            Assert.True(files > 10, "Suspiciously few files parsed");
            Assert.Contains(root.Children, static n => n is { Name: "PS3_GAME", IsDirectory: true });
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ChdPaths))]
    public void Iso9660BridgeParsesPs3Disc(string chdPath)
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
            var ok = new Iso9660Parser(reader).Parse(root);
            _output.WriteLine($"ISO9660 bridge parse: {(ok ? "OK" : "FAILED")}, top-level entries: {root.Children.Count}");
            Assert.True(ok, "Iso9660Parser failed on the PS3 UDF-bridge ISO part");

            foreach (var c in root.Children)
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

            var sfb = root.Children.FirstOrDefault(static n => n.Name == "PS3_DISC.SFB");
            Assert.NotNull(sfb);
            Assert.NotNull(sfb.ModifiedTime);

            var sec = new byte[2048];
            Assert.True(reader.ReadSector(sfb.Lba, sec));
            Assert.Equal(".SFB"u8.ToArray(), sec[..4]);
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ChdPaths))]
    public void ChdContentsMatchOriginalIso(string chdPath)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var container = new ChdContainer(chdPath);
        try
        {
            Assert.True(container.MountAndParse(ConsoleType.Ps3), "MountAndParse failed");

            foreach (var e in container.ListDirectory("\\"))
                _output.WriteLine($"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

            var all = CollectEntries(container, "\\").ToList();
            var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
            _output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
            foreach (var bad in badNames)
                _output.WriteLine($"BAD NAME: {bad.FullPath}");
            Assert.Empty(badNames);

            var magic = new byte[4];
            var sfb = container.FindFile("\\PS3_DISC.SFB");
            Assert.NotNull(sfb);
            Assert.Equal(4, container.ReadFile(sfb, 0, magic, 0, 4));
            Assert.Equal(".SFB"u8.ToArray(), magic);

            var sfo = container.FindFile(@"\PS3_GAME\PARAM.SFO");
            Assert.NotNull(sfo);
            Assert.Equal(4, container.ReadFile(sfo, 0, magic, 0, 4));
            Assert.Equal("\0PSF"u8.ToArray(), magic);

            var isoPath = Path.ChangeExtension(chdPath, ".iso");
            if (!File.Exists(isoPath))
            {
                _output.WriteLine("SKIP: no companion .iso for cross-validation");
                return;
            }

            using var iso = File.OpenRead(isoPath);

            var samples = fileEntries
                .Where(static e => e.Size > 0)
                .OrderByDescending(static e => e.Size)
                .Take(8)
                .Concat([sfo, sfb])
                .ToList();

            foreach (var entry in samples)
                VerifyHead(container, iso, entry);

            var multi = fileEntries.Where(static e => e.Extents.Count > 1).ToList();
            _output.WriteLine($"Multi-extent files: {multi.Count}");
            foreach (var entry in multi.Take(4))
            {
                ulong sum = 0;
                foreach (var x in entry.Extents)
                {
                    sum += x.Size;
                }

                Assert.Equal(entry.Size, sum);
                VerifyExtentBoundary(container, iso, entry);
            }
        }
        finally
        {
            container.Dispose();
        }
    }

    private void VerifyHead(ChdContainer container, FileStream iso, FileEntry entry)
    {
        var ext = entry.Extents.Count > 0 ? entry.Extents[0] : new FileExtent { Lba = entry.Lba, Size = entry.Size };
        var n = (int)Math.Min(65536, Math.Min(ext.Size, entry.Size));
        var chdBuf = new byte[n];
        Assert.Equal(n, container.ReadFile(entry, 0, chdBuf, 0, n));

        var isoBuf = new byte[n];
        iso.Position = (long)ext.Lba * 2048;
        iso.ReadExactly(isoBuf, 0, n);

        Assert.True(chdBuf.AsSpan().SequenceEqual(isoBuf), $"Data mismatch in head of {entry.FullPath}");
        _output.WriteLine($"OK head {n,6} bytes  {entry.FullPath}  (LBA {ext.Lba}, size {entry.Size:N0}, extents {entry.Extents.Count})");
    }

    private void VerifyExtentBoundary(ChdContainer container, FileStream iso, FileEntry entry)
    {
        var ext0 = entry.Extents[0];
        var ext1 = entry.Extents[1];
        var offset = ext0.Size - 2048;

        var chdBuf = new byte[4096];
        Assert.Equal(4096, container.ReadFile(entry, offset, chdBuf, 0, 4096));

        var isoBuf = new byte[4096];
        iso.Position = (long)ext0.Lba * 2048 + (long)offset;
        iso.ReadExactly(isoBuf, 0, 2048);
        iso.Position = (long)ext1.Lba * 2048;
        iso.ReadExactly(isoBuf, 2048, 2048);

        Assert.True(chdBuf.AsSpan().SequenceEqual(isoBuf), $"Data mismatch at extent boundary of {entry.FullPath}");
        _output.WriteLine($"OK extent boundary  {entry.FullPath}  (ext0 {ext0.Size:N0} @ {ext0.Lba} -> ext1 @ {ext1.Lba})");
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

    private static void Walk(FsNode node, ref int files, ref int dirs, ref int multi, ref ulong maxSize)
    {
        foreach (var c in node.Children)
        {
            if (c.IsDirectory)
            {
                dirs++;
                Walk(c, ref files, ref dirs, ref multi, ref maxSize);
            }
            else
            {
                files++;
                if (c.Extents.Count > 1)
                {
                    multi++;
                }

                if (c.Size > maxSize)
                {
                    maxSize = c.Size;
                }
            }
        }
    }
}
