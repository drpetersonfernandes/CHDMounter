using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class CDiDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public CDiDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DiagnosticCdiReadyDisc()
    {
        var sb = new StringBuilder();
        const string path = @"G:\MAME\MAME Software List CHDs\cdi\aliengat\alien gate (us, set 1)(cdi-ready).chd";

        if (!File.Exists(path)) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"=== {Path.GetFileName(path)} ===");
        var err = ChdFile.Open(path, out var chd);
        if (err != ChdError.Chderrnone || chd is null) return;

        try
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

            var reader = new SectorReader(chd, chd.UnitBytes);
            foreach (var t in reader.Tracks)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

            reader.Reset();

            sb.AppendLine("--- Reading raw sectors LBA 0-25 (unlocked, raw) ---");
            for (uint lba = 0; lba < 26; lba++)
            {
                if (reader.ReadRawSector(lba, out var raw))
                {
                    var syncHex = BitConverter.ToString(raw, 0, Math.Min(24, raw.Length));
                    var firstChars = new StringBuilder();
                    for (var i = 0; i < Math.Min(64, raw.Length); i++)
                    {
                        var c = (char)raw[i];
                        firstChars.Append(c is >= (char)32 and <= (char)126 ? c : '.');
                    }

                    sb.AppendLine(CultureInfo.InvariantCulture, $"  LBA={lba,3}: sync=[{syncHex}] chars=[{firstChars}]");
                }
            }

            sb.AppendLine("--- Now trying to read LBA 0-25 via ReadSector (2048 cooked) ---");
            var buf = new byte[2048];
            for (uint lba = 0; lba < 26; lba++)
            {
                if (reader.ReadSector(lba, buf))
                {
                    var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                    var sig4 = Encoding.ASCII.GetString(buf, 0, 5);
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  LBA={lba,3}: type={buf[0]:X2} sig1='{sig1}' sig4='{sig4}'");
                }
            }

            sb.AppendLine("--- Also try locked mode reading at LBA 0 ---");
            reader.Reset();
            var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
            if (dataTrack != null)
            {
                reader.SetTrack(dataTrack, true);
                sb.AppendLine(CultureInfo.InvariantCulture, $"Locked to track 1 (startLba={dataTrack.StartLba}), trying LBA 0: {reader.ReadSector(0, buf)}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"Locked, trying LBA 150: {reader.ReadSector(150, buf)}");
            }
        }
        finally
        {
            chd.Dispose();
        }

        Assert.Fail(sb.ToString());
    }

    [Fact]
    public void DiagnosticCdiWithIsoFallbackDiscs()
    {
        var paths = new[]
        {
            @"G:\MAME\MAME Software List CHDs\cdi\asspres2\from the associated press - the best of photo journalism (1993)[dvc].chd",
            @"G:\MAME\MAME Software List CHDs\cdi\photodem\photo cd demo disc v3.0 (1993)(philips)(eu)[1993-03].chd",
            @"G:\MAME\MAME Software List CHDs\cdi\pcd1904\pcd1904.chd"
        };

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            _output.WriteLine($"=== {Path.GetFileName(path)} ===");
            var err = ChdFile.Open(path, out var chd);
            if (err != ChdError.Chderrnone || chd is null) continue;

            try
            {
                _output.WriteLine($"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

                var reader = new SectorReader(chd, chd.UnitBytes);
                _output.WriteLine($"Tracks: {reader.Tracks.Count}");

                foreach (var t in reader.Tracks)
                    _output.WriteLine($"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

                reader.Reset();
                var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
                if (dataTrack == null)
                {
                    _output.WriteLine("  No data track found!");
                    continue;
                }

                reader.SetTrack(dataTrack, true);

                for (uint offset = 0; offset < Math.Min(200u, dataTrack.Frames); offset++)
                {
                    var lba = dataTrack.StartLba + offset;
                    var buf = new byte[2048];
                    if (!reader.ReadSector(lba, buf)) continue;

                    var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                    var sig2 = Encoding.ASCII.GetString(buf, 0, 5);
                    if (offset < 30 || sig1 is "CD-I " or "CD001" || sig2 is "CD001")
                        _output.WriteLine($"  LBA={lba} (offset {offset}): type={buf[0]:X2} sig1='{sig1}' sig2='{sig2}'");
                }
            }
            finally
            {
                chd.Dispose();
            }
        }
    }

    [Fact]
    public void DiagnosticMusicCdiDiscs()
    {
        var sb = new StringBuilder();
        var paths = new[]
        {
            @"I:\Philips CD-i\Pavarotti - O Sole Mio (USA).chd",
            @"I:\Philips CD-i\James Brown - Non Stop Hit Machine (USA).chd"
        };

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            sb.AppendLine(CultureInfo.InvariantCulture, $"=== {Path.GetFileName(path)} ===");
            var err = ChdFile.Open(path, out var chd);
            if (err != ChdError.Chderrnone || chd is null) continue;

            try
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

                var reader = new SectorReader(chd, chd.UnitBytes);
                sb.AppendLine(CultureInfo.InvariantCulture, $"Tracks: {reader.Tracks.Count}");

                foreach (var t in reader.Tracks)
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

                reader.Reset();
                var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
                if (dataTrack == null)
                {
                    sb.AppendLine("  No data track found!");
                    continue;
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"Data track: {dataTrack.Index} type={dataTrack.TrackType} startLba={dataTrack.StartLba} frames={dataTrack.Frames} pregap={dataTrack.Pregap}");

                for (uint lba = 0; lba < 30; lba++)
                {
                    if (reader.ReadRawSector(lba, out var raw))
                    {
                        var syncHex = BitConverter.ToString(raw, 0, Math.Min(16, raw.Length));
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Raw LBA={lba,3}: sync=[{syncHex}]");
                    }
                }

                reader.Reset();
                reader.SetTrack(dataTrack, true);
                var buf = new byte[2048];

                sb.AppendLine(CultureInfo.InvariantCulture, $"Reading locked from track start LBA {dataTrack.StartLba}:");
                for (uint offset = 0; offset < 30; offset++)
                {
                    var lba = dataTrack.StartLba + offset;
                    if (reader.ReadSector(lba, buf))
                    {
                        var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                        var sig0 = Encoding.ASCII.GetString(buf, 0, 5);
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  LBA={lba} off={offset}: type={buf[0]:X2} sig1='{sig1}' sig0='{sig0}'");
                    }
                }

                sb.AppendLine("Trying CDiFsParser:");
                reader.Reset();
                var root = new FsNode();
                var parser = new CDiFsParser(reader);
                var ok = parser.Parse(root, dataTrack);
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Parse result: {ok}, children: {root.Children.Count}");
            }
            finally
            {
                chd.Dispose();
            }
        }

        Assert.Fail(sb.ToString());
    }
}
