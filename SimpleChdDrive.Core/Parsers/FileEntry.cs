namespace SimpleChdDrive.Core.Parsers;

public class FileEntry
{
    public string Name { get; set; } = string.Empty;
    public uint Lba { get; set; }
    public ulong Size { get; set; }
    public ulong Offset { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime ModifiedTime { get; set; } = DateTime.Now;
    public byte FileNumber { get; set; }
    public bool IsInterleaved { get; set; }
    public List<FileExtent> Extents { get; set; } = [];
    public List<FileEntry> Children { get; set; } = [];
}

public struct FileExtent
{
    public uint Lba;
    public ulong Size;
}
