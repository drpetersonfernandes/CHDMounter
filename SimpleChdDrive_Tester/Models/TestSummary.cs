namespace Tester.Models;

internal sealed class TestSummary
{
    public string ConsoleName { get; set; } = "";
    public string ChdFolder { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<TestResult> Results { get; set; } = [];
    public int TotalFiles => Results.Count;
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailCount => Results.Count(r => !r.Success);
    public TimeSpan TotalDuration => EndTime - StartTime;
    public long TotalBytes => Results.Where(r => r.Success).Sum(r => (long)r.VolumeSize);
    public int TotalEntries => Results.Where(r => r.Success).Sum(r => r.FileCount + r.DirectoryCount);
    public TimeSpan AverageDuration => Results.Count > 0
        ? TimeSpan.FromMilliseconds(Results.Average(r => r.Duration.TotalMilliseconds))
        : TimeSpan.Zero;
    public TestResult? Fastest => Results.Count > 0 ? Results.MinBy(r => r.Duration) : null;
    public TestResult? Slowest => Results.Count > 0 ? Results.MaxBy(r => r.Duration) : null;
}
