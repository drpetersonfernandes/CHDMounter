namespace VideoGameFileSystemParser.Models;

/// <summary>
/// Identifies the type of a file system node.
/// </summary>
public enum FsNodeType
{
    /// <summary>A regular file.</summary>
    File = 0,
    /// <summary>A directory.</summary>
    Directory = 4,
    /// <summary>A symbolic link.</summary>
    Symlink = 12
}

/// <summary>
/// Represents a node in the parsed file system tree.
/// </summary>
public class FsNode
{
    /// <summary>
    /// The file or directory name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The LBA of the first extent.
    /// </summary>
    public uint Lba { get; set; }
    /// <summary>
    /// The total data size in bytes.
    /// </summary>
    public ulong Size { get; set; }
    /// <summary>
    /// The file number for interleaved (XA) access.
    /// </summary>
    public byte FileNumber { get; set; }
    /// <summary>
    /// Whether data is interleaved.
    /// </summary>
    public bool IsInterleaved { get; set; }
    /// <summary>
    /// Whether this node is a directory.
    /// </summary>
    public bool IsDirectory { get; set; }
    /// <summary>
    /// Whether this node spans multiple extents.
    /// </summary>
    public bool IsMultiExtent { get; set; }
    /// <summary>
    /// Whether to read data as raw bytes.
    /// </summary>
    public bool IsRawPassthrough { get; set; }
    /// <summary>
    /// Whether data is embedded within a file entry sector.
    /// </summary>
    public bool IsEmbedded { get; set; }
    /// <summary>
    /// The byte offset within the sector for embedded data.
    /// </summary>
    public uint EmbeddedOffset { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public DateTime? CreatedTime { get; set; }
    public DateTime? AccessedTime { get; set; }
    public uint? UnixMode { get; set; }
    public uint? Uid { get; set; }
    public uint? Gid { get; set; }
    public uint? Inode { get; set; }
    public uint? LinkCount { get; set; }
    /// <summary>
    /// The type of this node (file, directory, or symlink).
    /// </summary>
    public FsNodeType NodeType { get; set; } = FsNodeType.File;
    public string? SymlinkTarget { get; set; }
    public List<FsExtent> Extents { get; set; } = [];
    public List<FsNode> Children { get; set; } = [];
}

/// <summary>
/// Represents a contiguous data extent with a starting LBA and size.
/// </summary>
public struct FsExtent
{
    /// <summary>
    /// The starting logical block address.
    /// </summary>
    public uint Lba { get; set; }
    /// <summary>
    /// The size in bytes.
    /// </summary>
    public ulong Size { get; set; }
}
