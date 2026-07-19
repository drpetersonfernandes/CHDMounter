using System.Diagnostics;
using Xunit.Abstractions;

namespace SimpleChdDrive.Core.Tests.Parsers;

public static class SequentialTestRunner
{
    public static List<string> CollectPaths(params string[] directories)
    {
        var paths = new List<string>();
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir)) continue;

            paths.AddRange(Directory.EnumerateFiles(dir, "*.chd", SearchOption.AllDirectories));
        }

        return paths;
    }

    public static List<string> CollectPaths(IEnumerable<string> directories)
    {
        return CollectPaths(directories.ToArray());
    }

    public static void Run(ITestOutputHelper output, string testName, List<string> chdPaths,
        Func<string, ITestOutputHelper, bool> testFunc)
    {
        var failures = new List<(string path, string error)>();
        var sw = Stopwatch.StartNew();
        int passed = 0, skipped = 0;

        foreach (var chdPath in chdPaths)
        {
            if (!File.Exists(chdPath))
            {
                output.WriteLine($"SKIP: {chdPath} not found");
                skipped++;
                continue;
            }

            var fileName = Path.GetFileName(chdPath);
            try
            {
                output.WriteLine($"--- {fileName} ---");
                if (testFunc(chdPath, output))
                {
                    passed++;
                }
                else
                    failures.Add((chdPath, $"{testName} returned false for {fileName}"));
            }
            catch (Exception ex)
            {
                failures.Add((chdPath, $"{ex.GetType().Name}: {ex.Message}"));
                output.WriteLine($"  FAIL: {ex.GetType().Name}: {ex.Message}");
            }
        }

        sw.Stop();
        output.WriteLine(
            $"{testName}: {passed} passed, {skipped} skipped, {failures.Count} failed in {sw.Elapsed.TotalSeconds:F1}s");

        Assert.True(failures.Count == 0,
            $"{failures.Count} failures in {testName}:\n" +
            string.Join('\n', failures.Select(static f => $"  {Path.GetFileName(f.path)} - {f.error}")));
    }
}
