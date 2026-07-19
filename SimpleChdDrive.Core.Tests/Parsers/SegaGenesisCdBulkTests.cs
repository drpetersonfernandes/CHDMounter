using System.Diagnostics;
using CHDSharp;
using CHDSharp.Models;
using SimpleChdDrive.Core.Parsers.Systems;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class SegaGenesisCdBulkTests
{
    private static readonly string[] LibraryPaths =
    [
        @"G:\MAME\MAME Software List CHDs\megacd",
        @"I:\Sega Genesis CD"
    ];

    private readonly ITestOutputHelper _output;

    public SegaGenesisCdBulkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseEntireMameMegacdLibrary()
    {
        const string path = @"G:\MAME\MAME Software List CHDs\megacd";

        if (!Directory.Exists(path))
        {
            _output.WriteLine($"SKIP: {path} not found");
            return;
        }

        var chdFiles = Directory.EnumerateFiles(path, "*.chd", SearchOption.AllDirectories).OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {chdFiles.Count} CHD files (MAME megacd)");
        Assert.NotEmpty(chdFiles);

        RunBulkTest(chdFiles);
    }

    [Fact]
    public void ParseEntireISegaGenesisCdLibrary()
    {
        const string path = @"I:\Sega Genesis CD";

        if (!Directory.Exists(path))
        {
            _output.WriteLine($"SKIP: {path} not found");
            return;
        }

        var chdFiles = Directory.EnumerateFiles(path, "*.chd", SearchOption.TopDirectoryOnly).OrderBy(static f => f).ToList();
        _output.WriteLine($"Found {chdFiles.Count} CHD files (I: Sega Genesis CD)");
        Assert.NotEmpty(chdFiles);

        RunBulkTest(chdFiles);
    }

    [Fact]
    public void ParseAllSegaGenesisCdLibraries()
    {
        var allChdFiles = new List<string>();

        foreach (var libPath in LibraryPaths)
        {
            if (!Directory.Exists(libPath))
            {
                _output.WriteLine($"SKIP: {libPath} not found");
                continue;
            }

            var searchOpt = libPath.Contains("megacd", StringComparison.OrdinalIgnoreCase)
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
                var parser = new SegaGenesisCdParser(reader);

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
        {
            count += c.IsDirectory ? CountFiles(c) : 1;
        }

        return count;
    }
}
