using System.Diagnostics;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class CDiBulkTests
{
    private static readonly string[] LibraryPaths =
    [
        @"G:\MAME\MAME Software List CHDs\cdi",
        @"I:\Philips CD-i"
    ];

    private readonly ITestOutputHelper _output;

    public CDiBulkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseEntireMameCdiLibrary()
    {
        var path = @"G:\MAME\MAME Software List CHDs\cdi";

        if (!Directory.Exists(path))
        {
            _output.WriteLine($"SKIP: {path} not found");
            return;
        }

        var chdFiles = Directory.EnumerateFiles(path, "*.chd", SearchOption.AllDirectories).OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {chdFiles.Count} CHD files (MAME cdi)");
        Assert.NotEmpty(chdFiles);

        RunBulkTest(chdFiles);
    }

    [Fact]
    public void ParseEntireIPhilipsCdiLibrary()
    {
        var path = @"I:\Philips CD-i";

        if (!Directory.Exists(path))
        {
            _output.WriteLine($"SKIP: {path} not found");
            return;
        }

        var chdFiles = Directory.EnumerateFiles(path, "*.chd", SearchOption.TopDirectoryOnly).OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {chdFiles.Count} CHD files (I: Philips CD-i)");
        Assert.NotEmpty(chdFiles);

        RunBulkTest(chdFiles);
    }

    [Fact]
    public void ParseAllCdiLibraries()
    {
        var allChdFiles = new List<string>();

        foreach (var libPath in LibraryPaths)
        {
            if (!Directory.Exists(libPath))
            {
                _output.WriteLine($"SKIP: {libPath} not found");
                continue;
            }

            var searchOpt = libPath.Contains("cdi", StringComparison.OrdinalIgnoreCase) && !libPath.Contains("CD-i", StringComparison.OrdinalIgnoreCase)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.EnumerateFiles(libPath, "*.chd", searchOpt);
            allChdFiles.AddRange(files);
        }

        allChdFiles = allChdFiles.OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {allChdFiles.Count} CHD files across all libraries");
        Assert.NotEmpty(allChdFiles);

        RunBulkTest(allChdFiles);
    }

    [Fact]
    public void ParseCdiLibraryWithIso9660Fallback()
    {
        var allChdFiles = new List<string>();

        foreach (var libPath in LibraryPaths)
        {
            if (!Directory.Exists(libPath))
            {
                _output.WriteLine($"SKIP: {libPath} not found");
                continue;
            }

            var searchOpt = libPath.Contains("cdi", StringComparison.OrdinalIgnoreCase) && !libPath.Contains("CD-i", StringComparison.OrdinalIgnoreCase)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            allChdFiles.AddRange(Directory.EnumerateFiles(libPath, "*.chd", searchOpt));
        }

        allChdFiles = allChdFiles.OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {allChdFiles.Count} CHD files across all libraries");
        Assert.NotEmpty(allChdFiles);

        var cdiFailures = new List<string>();
        var isoFailures = new List<string>();
        int cdiParsed = 0, cdiFiles = 0;
        int isoParsed = 0, isoFiles = 0;
        var sw = Stopwatch.StartNew();

        foreach (var chdPath in allChdFiles)
        {
            var name = Path.GetFileName(chdPath);
            ChdFile? chd = null;

            try
            {
                var err = ChdFile.Open(chdPath, out chd);
                if (err != ChdError.Chderrnone || chd is null)
                {
                    cdiFailures.Add($"{name}: CHD open failed ({err})");
                    continue;
                }

                var reader = new SectorReader(chd, chd.UnitBytes);
                var root = new FsNode();
                var parser = new CDiParser(reader);

                if (!parser.Parse(root))
                {
                    var reason = $"tracks={reader.Tracks.Count}, dataTracks={reader.Tracks.Count(static t => t.IsDataTrack)}";
                    var isoRoot = new FsNode();
                    var isoParser = new Iso9660Parser(reader);
                    var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
                    var isoOk = isoParser.Parse(isoRoot, track);
                    var isoFileCount = CountFiles(isoRoot);

                    if (isoOk && isoFileCount > 0)
                    {
                        isoParsed++;
                        isoFiles += isoFileCount;
                        _output.WriteLine($"  ISO fallback OK: {name} ({isoFileCount} files)");
                    }
                    else
                    {
                        cdiFailures.Add($"{name}: CDi parse failed ({reason}), ISO also failed ({isoFileCount} files)");
                    }

                    continue;
                }

                var files = CountFiles(root);
                if (files == 0)
                {
                    cdiFailures.Add($"{name}: parsed but 0 files");
                    continue;
                }

                cdiParsed++;
                cdiFiles += files;
            }
            catch (Exception ex)
            {
                cdiFailures.Add($"{name}: EXCEPTION {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                chd?.Dispose();
            }
        }

        sw.Stop();
        _output.WriteLine($"CDi parsed: {cdiParsed}/{allChdFiles.Count} ({cdiFiles:N0} files)");
        _output.WriteLine($"ISO fallback: {isoParsed} ({isoFiles:N0} files)");
        _output.WriteLine($"Total OK: {cdiParsed + isoParsed}/{allChdFiles.Count} in {sw.Elapsed.TotalSeconds:F1}s");
        _output.WriteLine($"CDi failures: {cdiFailures.Count}");

        foreach (var f in cdiFailures.Take(60))
            _output.WriteLine($"  FAIL: {f}");
        if (cdiFailures.Count > 60)
            _output.WriteLine($"  ... and {cdiFailures.Count - 60} more");

        Assert.Empty(cdiFailures);
    }

    private void RunBulkTest(List<string> chdFiles)
    {
        var failures = new List<string>();
        int parsed = 0, totalFiles = 0;
        var sw = Stopwatch.StartNew();

        foreach (var chdPath in chdFiles)
        {
            var name = Path.GetFileName(chdPath);
            ChdFile? chd = null;

            try
            {
                var err = ChdFile.Open(chdPath, out chd);
                if (err != ChdError.Chderrnone || chd is null)
                {
                    failures.Add($"{name}: CHD open failed ({err})");
                    continue;
                }

                var reader = new SectorReader(chd, chd.UnitBytes);
                var root = new FsNode();
                var parser = new CDiParser(reader);

                if (!parser.Parse(root))
                {
                    failures.Add($"{name}: parse failed (tracks={reader.Tracks.Count}, dataTracks={reader.Tracks.Count(static t => t.IsDataTrack)})");
                    continue;
                }

                var files = CountFiles(root);
                if (files == 0)
                {
                    failures.Add($"{name}: parsed but 0 files");
                    continue;
                }

                parsed++;
                totalFiles += files;
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: EXCEPTION {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                chd?.Dispose();
            }
        }

        sw.Stop();
        _output.WriteLine($"Parsed OK: {parsed}/{chdFiles.Count} ({totalFiles:N0} files total) in {sw.Elapsed.TotalSeconds:F1}s");
        _output.WriteLine($"Failures: {failures.Count}");

        foreach (var f in failures.Take(60))
            _output.WriteLine($"  FAIL: {f}");
        if (failures.Count > 60)
            _output.WriteLine($"  ... and {failures.Count - 60} more");

        Assert.Empty(failures);
    }

    private static int CountFiles(FsNode node)
    {
        var count = 0;
        foreach (var c in node.Children)
            count += c.IsDirectory ? CountFiles(c) : 1;
        return count;
    }
}
