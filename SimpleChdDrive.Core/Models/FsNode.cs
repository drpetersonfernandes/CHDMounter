namespace SimpleChdDrive.Core.Models;

public class FsNode
{
    public string Name { get; set; } = string.Empty;
    public uint Lba { get; set; }
    public ulong Size { get; set; }
    public byte FileNumber { get; set; }
    public bool IsInterleaved { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsMultiExtent { get; set; }
    public bool IsRawPassthrough { get; set; }
    public bool IsEmbedded { get; set; }
    public uint EmbeddedOffset { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public List<FsExtent> Extents { get; set; } = [];
    public List<FsNode> Children { get; set; } = [];
}

public struct FsExtent
{
    public uint Lba;
    public ulong Size;
}
