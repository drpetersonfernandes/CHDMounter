namespace SimpleChdDrive.Core.CHD.Flac;

public struct SeekPoint
{
    public long Number { get; set; }
    public long Offset { get; set; }
    public int Framesize { get; set; }
}