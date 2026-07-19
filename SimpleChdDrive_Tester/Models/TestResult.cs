namespace Tester.Models;

public sealed record TestResult(
    string FileName,
    string FilePath,
    bool Success,
    string ErrorMessage,
    string VolumeName,
    ulong VolumeSize,
    int FileCount,
    int DirectoryCount,
    TimeSpan Duration
);
