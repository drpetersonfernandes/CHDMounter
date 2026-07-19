namespace VideoGameFileSystemParser.Models;

public enum FsNodeType
{
    File = 0,
    Directory = 4,
    Symlink = 12
}

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
    public DateTime? CreatedTime { get; set; }
    public DateTime? AccessedTime { get; set; }
    public uint? UnixMode { get; set; }
    public uint? Uid { get; set; }
    public uint? Gid { get; set; }
    public uint? Inode { get; set; }
    public uint? LinkCount { get; set; }
    public FsNodeType NodeType { get; set; } = FsNodeType.File;
    public string? SymlinkTarget { get; set; }
    public List<FsExtent> Extents { get; set; } = [];
    public List<FsNode> Children { get; set; } = [];
}

public struct FsExtent
{
    public uint Lba;
    public ulong Size;
}
