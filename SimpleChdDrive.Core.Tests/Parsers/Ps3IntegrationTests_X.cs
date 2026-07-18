using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class Ps3IntegrationTestsX
{
    private readonly ITestOutputHelper _output;

    public Ps3IntegrationTestsX(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> Ps3ChdPaths =>
    [
        @"X:\Sony PlayStation 3\007 - Blood Stone (USA) (En,Fr).chd",
        @"X:\Sony PlayStation 3\007 - Quantum of Solace (USA) (En,Fr).chd",
        @"X:\Sony PlayStation 3\007 Legends (USA) (En,Fr).chd",
        @"X:\Sony PlayStation 3\2010 FIFA World Cup South Africa (USA, Asia) (En,Fr,Es).chd",
        @"X:\Sony PlayStation 3\3D Dot Game Heroes (USA).chd"
    ];

    [Theory]
    [MemberData(nameof(Ps3ChdPaths))]
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

            _output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track?.TrackType ?? "N/A"}");

            var root = new FsNode();
            var parser = new UdfParser(reader);

            var ok = parser.Parse(root, track);
            _output.WriteLine($"UdfParser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "UdfParser could not parse the disc");

            int files = 0, dirs = 0, multiExtent = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref multiExtent, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, {multiExtent} multi-extent files, largest file {maxSize:N0} bytes");

            var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
            foreach (var c in topTwenty)
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");
            Assert.Contains(root.Children, static n => n is { Name: "PS3_GAME", IsDirectory: true });
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Ps3ChdPaths))]
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
    [MemberData(nameof(Ps3ChdPaths))]
    public void PlayStation3ParserParsesPs3Disc(string chdPath)
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
            var parser = new PlayStation3Parser(reader);

            var ok = parser.Parse(root);
            _output.WriteLine($"PlayStation3Parser: {(ok ? "OK" : "FAILED")}");

            Assert.True(ok, "PlayStation3Parser could not parse the disc");

            int files = 0, dirs = 0, multiExtent = 0;
            ulong maxSize = 0;
            Walk(root, ref files, ref dirs, ref multiExtent, ref maxSize);
            _output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, {multiExtent} multi-extent files, largest file {maxSize:N0} bytes");

            Assert.True(files > 10, $"Suspiciously few files parsed: {files}");
            Assert.Contains(root.Children, static n => n is { Name: "PS3_GAME", IsDirectory: true });
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Ps3ChdPaths))]
    public void ChdContainerMountAndParsePs3Disc(string chdPath)
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

            Assert.True(fileEntries.Count > 10, $"Suspiciously few files: {fileEntries.Count}");

            var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
            foreach (var bad in badNames)
                _output.WriteLine($"BAD NAME: {bad.FullPath}");
            Assert.Empty(badNames);

            var magic = new byte[4];

            var sfb = container.FindFile(@"\PS3_DISC.SFB");
            if (sfb != null)
            {
                Assert.Equal(4, container.ReadFile(sfb, 0, magic, 0, 4));
                Assert.Equal(".SFB"u8.ToArray(), magic);
                _output.WriteLine("PS3_DISC.SFB: OK");
            }
            else
            {
                _output.WriteLine("PS3_DISC.SFB: NOT FOUND");
            }

            var sfo = container.FindFile(@"\PS3_GAME\PARAM.SFO");
            if (sfo != null)
            {
                Assert.Equal(4, container.ReadFile(sfo, 0, magic, 0, 4));
                Assert.Equal("\0PSF"u8.ToArray(), magic);
                _output.WriteLine("PARAM.SFO: OK");

                var sfoBuf = new byte[(int)Math.Min(sfo.Size, 2048)];
                var sfoLen = container.ReadFile(sfo, 0, sfoBuf, 0, sfoBuf.Length);
                var title = ReadSfoString(sfoBuf, sfoLen);
                if (title != null)
                    _output.WriteLine($"  TITLE_ID: {title}");
            }
            else
            {
                _output.WriteLine("PARAM.SFO: NOT FOUND");
            }
        }
        finally
        {
            container.Dispose();
        }
    }

    private static string? ReadSfoString(byte[] buf, int length)
    {
        try
        {
            if (length < 20) return null;

            var keyTableStart = BitConverter.ToUInt32(buf, 8);
            var dataTableStart = BitConverter.ToUInt32(buf, 16);
            var numEntries = BitConverter.ToUInt32(buf, 20);

            for (uint i = 0; i < numEntries; i++)
            {
                var pos = (int)(20 + i * 16);
                if (pos + 16 > length) break;

                var keyOff = BitConverter.ToUInt16(buf, pos);
                var dataOff = BitConverter.ToUInt32(buf, pos + 8);
                var dataLen = BitConverter.ToUInt32(buf, pos + 12);

                var keyEnd = Array.IndexOf<byte>(buf, 0, (int)(keyTableStart + keyOff));
                if (keyEnd < 0)
                {
                    keyEnd = length;
                }

                var key = Encoding.ASCII.GetString(buf, (int)(keyTableStart + keyOff), keyEnd - (int)(keyTableStart + keyOff));

                if (key == "TITLE_ID")
                {
                    var dataPos = (int)(dataTableStart + dataOff);
                    var dLen = (int)Math.Min(dataLen, (uint)(length - dataPos));
                    return Encoding.ASCII.GetString(buf, dataPos, dLen).TrimEnd('\0');
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
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
