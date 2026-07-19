using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Parsing.Parsers;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class PcFxDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public PcFxDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string, string> PcFxCompareChds => new()
    {
        { @"G:\MAME\MAME Software List CHDs\pcfx\dknight4\dragon knight 4 (japan).chd", "FAILING - MAME DK4" },
        { @"G:\NEC PC-FX\Dragon Knight 4 (Japan).chd", "FAILING - NEC DK4" },
        { @"G:\MAME\MAME Software List CHDs\pcfx\batlheat\battle heat (japan).chd", "FAILING - MAME BattleHeat" },
        { @"G:\NEC PC-FX\Battle Heat (Japan).chd", "FAILING - NEC BattleHeat" },
        { @"G:\MAME\MAME Software List CHDs\pcfx\farland\farland story fx (japan).chd", "FAILING - MAME Farland" },
        { @"G:\NEC PC-FX\AnimeFreak FX Vol. 1 (Japan).chd", "FAILING - AnimeFreak" },
        { @"G:\NEC PC-FX\Pia Carrot e Youkoso! We've Been Waiting for You (Japan).chd", "PASSING - Pia Carrot" },
        { @"G:\NEC PC-FX\Sotsugyou II FX - Neo Generation (Japan) (SABS, SACS).chd", "PASSING - Sotsugyou" },
        { @"G:\NEC PC-FX\Super PC Engine Fan Deluxe - Special CD-ROM Vol. 1 (Japan).chd", "PASSING - Super PCEFan" },
        { @"G:\NEC PC-FX\Team Innocent - The Point of No Return - G.C.P.O.SS (Japan).chd", "PASSING - Team Innocent" }
    };

    [Theory]
    [MemberData(nameof(PcFxCompareChds))]
    public void DiagnoseChdMetadata(string chdPath, string label)
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
            _output.WriteLine($"=== {label} ===");
            _output.WriteLine($"  CHD: hunkSize={chd.HunkBytes}, unitBytes={chd.UnitBytes}");

            foreach (var meta in chd.Metadata)
                _output.WriteLine($"  Meta: tag='{meta.Tag}' text='{meta.GetText()}'");

            var reader = new SectorReader(chd, chd.UnitBytes);
            foreach (var t in reader.Tracks)
                _output.WriteLine($"  Track[{t.Index}]: Type='{t.TrackType}' IsData={t.IsDataTrack} Frames={t.Frames} StartLba={t.StartLba} ChdOffset={t.ChdOffset} Pregap={t.Pregap}");

            var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
            if (track == null) { _output.WriteLine("  NO TRACK!");
                return; }

            var root = new FsNode();
            var parser = new Iso9660Parser(reader);
            var ok = parser.Parse(root, track);
            _output.WriteLine($"  Iso9660Parser: {(ok ? "OK" : "FAILED")}");

            if (!ok)
            {
                _output.WriteLine($"  SectorHeaderOffset={reader.SectorHeaderOffset} SyncOffset={reader.SyncOffset}");

                // Scan data track for CD001 (raw and descrambled)
                var sectorsPerHunk = chd.HunkBytes / chd.UnitBytes;
                var scramble = SectorReader.GetSectorScramble();
                var cd001 = "CD001"u8.ToArray();
                uint found = 0;
                var hunkBuf = new byte[chd.HunkBytes];
                var lastHunk = 0xFFFFFFFF;
                var endFrame = track.ChdOffset + Math.Min(track.Frames, 50000u);
                for (var frame = track.ChdOffset; frame < endFrame && found < 3; frame++)
                {
                    var h = frame / sectorsPerHunk;
                    var s = frame % sectorsPerHunk;
                    if (h != lastHunk)
                    {
                        if (chd.ReadHunk(h, hunkBuf) != ChdError.Chderrnone) continue;

                        lastHunk = h;
                    }
                    var secOff = (int)(s * chd.UnitBytes);
                    if (secOff + 22 > hunkBuf.Length) continue;

                    bool rawOk = true, descOk = true;
                    for (var j = 0; j < 5; j++)
                    {
                        if (hunkBuf[secOff + 17 + j] != cd001[j])
                        {
                            rawOk = false;
                        }

                        if (scramble.Length > 17 + j && (hunkBuf[secOff + 17 + j] ^ scramble[17 + j]) != cd001[j])
                        {
                            descOk = false;
                        }

                        if (!rawOk && !descOk) break;
                    }
                    if (rawOk || descOk)
                    {
                        var msf = $"{hunkBuf[secOff + 12]:X2}:{hunkBuf[secOff + 13]:X2}:{hunkBuf[secOff + 14]:X2}";
                        _output.WriteLine($"    {(rawOk ? "RAW" : "DESCRAM")} CD001 at frame={frame} MSF={msf}");
                        found++;
                    }
                }
                if (found == 0) _output.WriteLine("    CD001 NOT FOUND in first 50k frames");
            }
            else
            {
                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                _output.WriteLine($"  {files} files, {dirs} dirs");
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
