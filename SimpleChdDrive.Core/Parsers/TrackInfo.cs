namespace SimpleChdDrive.Core.Parsers;

public class TrackInfo
{
    public int Index { get; set; }
    public uint StartLBA { get; set; }
    public uint ChdOffset { get; set; }
    public uint Frames { get; set; }
    public string TrackType { get; set; } = string.Empty;
    public bool IsDataTrack { get; set; }
    public uint Pregap { get; set; }
    public uint Postgap { get; set; }
    public string Metadata { get; set; } = string.Empty;
}
