using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class PspPsnIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PspPsnIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string> PspPsnChdPaths =>
    [
        @"X:\Sony PSP PSN\101 in 1 Megamix (USA) (En,Fr,Es).chd",
        @"X:\Sony PSP PSN\2010 FIFA World Cup South Africa (USA) (En,Es).chd",
        @"X:\Sony PSP PSN\300 - March to Glory (USA).chd",
        @"X:\Sony PSP PSN\7 Wonders of the Ancient World (USA).chd",
        @"X:\Sony PSP PSN\Adventures to Go! (USA).chd"
    ];

    [Theory]
    [MemberData(nameof(PspPsnChdPaths))]
    public void Iso9660ParserParsesPspPsnDisc(string chdPath)
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

            Assert.True(files > 2, $"Suspiciously few files parsed: {files}");

            var hasPspGame = root.Children.Any(static n => n is { Name: "PSP_GAME", IsDirectory: true });
            var hasUmdDataBin = root.Children.Any(static n => n.Name == "UMD_DATA.BIN");
            _output.WriteLine($"PSP_GAME: {(hasPspGame ? "YES" : "NO")}  UMD_DATA.BIN: {(hasUmdDataBin ? "YES" : "NO")}");

            foreach (var c in root.Children.OrderByDescending(static n => n.Size).Take(10))
                _output.WriteLine($"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}");
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
            if (c.IsDirectory) { dirs++;
                Walk(c, ref files, ref dirs, ref maxSize); }
            else { files++;
                if (c.Size > maxSize)
                {
                    maxSize = c.Size;
                }
            }
        }
    }
}
