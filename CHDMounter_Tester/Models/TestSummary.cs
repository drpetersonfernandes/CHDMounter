namespace Tester.Models;

internal sealed class TestSummary
{
    internal string ConsoleName { get; set; } = "";
    internal string ChdFolder { get; set; } = "";
    internal DateTime StartTime { get; set; }
    internal DateTime EndTime { get; set; }
    internal List<TestResult> Results { get; set; } = [];
    internal int TotalFiles => Results.Count;
    internal int SuccessCount => Results.Count(r => r.Success);
    internal int FailCount => Results.Count(r => !r.Success);
    internal TimeSpan TotalDuration => EndTime - StartTime;
    internal long TotalBytes => Results.Where(r => r.Success).Sum(r => (long)r.VolumeSize);
    internal int TotalEntries => Results.Where(r => r.Success).Sum(r => r.FileCount + r.DirectoryCount);
    internal TimeSpan AverageDuration => Results.Count > 0
        ? TimeSpan.FromMilliseconds(Results.Average(r => r.Duration.TotalMilliseconds))
        : TimeSpan.Zero;
    internal TestResult? Fastest => Results.Count > 0 ? Results.MinBy(r => r.Duration) : null;
    internal TestResult? Slowest => Results.Count > 0 ? Results.MaxBy(r => r.Duration) : null;
}
