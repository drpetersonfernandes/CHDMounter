namespace SimpleChdDrive.Core.CHD;

internal static class ChdCommon
{
    internal static ChdCodecType CompTypeConv(uint ct)
    {
        switch (ct)
        {
            case 1:
            case 2: return ChdCodecType.Zlib;
            case 3: return ChdCodecType.Avhu;
            default:
                return ChdCodecType.Error;
        }
    }

    /* Converts V3 & V4 mapFlags to V5 compression_type */
    internal static CompressionType ConvMapFlagstoCompressionType(MapFlag mapFlags)
    {
        switch (mapFlags & MapFlag.TypeMask)
        {
            case MapFlag.Invalid: return CompressionType.Compressionerror;
            case MapFlag.Compressed: return CompressionType.Compressiontype0;
            case MapFlag.Uncompressed: return CompressionType.Compressionnone;
            case MapFlag.Mini: return CompressionType.Compressionmini;
            case MapFlag.SelfHunk: return CompressionType.Compressionself;
            case MapFlag.ParentHunk: return CompressionType.Compressionparent;
            default:
                return CompressionType.Compressionerror;
        }
    }
}

public enum ChdCodecType
{
    None = 0,
    Zlib = 0x7A6C6962, // zlib
    Lzma = 0x6C7A6D61, // lzma
    Huffman = 0x68756666, // huff
    Flac = 0x666C6163, // flac
    Zstd = 0x7A737464, // zstd
    Cdzl = 0x63647A6C, // cdzl
    Cdlz = 0x63646C7A, // cdlz
    Cdfl = 0x6364666C, // cdfl
    Cdzs = 0x63647A73, // cdzs
    Avhu = 0x61766875, // avhu
    Error = 0x0eeeeeee
}

[Flags]
public enum MapFlag
{
    TypeMask = 0x000f,      /* what type of hunk */
    NoCrc = 0x0010,         /* no CRC is present */

    Invalid = 0x0000,        /* invalid type */
    Compressed = 0x0001,     /* standard compression */
    Uncompressed = 0x0002,   /* uncompressed data */
    Mini = 0x0003,           /* mini: use offset as raw data */
    SelfHunk = 0x0004,      /* same as another hunk in this file */
    ParentHunk = 0x0005     /* same as a hunk in the parent file */
}

public enum CompressionType
{
    /* codec #0
     * these types are live when running */
    Compressiontype0 = 0,
    /* codec #1 */
    Compressiontype1 = 1,
    /* codec #2 */
    Compressiontype2 = 2,
    /* codec #3 */
    Compressiontype3 = 3,
    /* no compression; implicit length = hunkbytes */
    Compressionnone = 4,
    /* same as another block in this chd */
    Compressionself = 5,
    /* same as a hunk's worth of units in the parent chd */
    Compressionparent = 6,

    /* start of small RLE run (4-bit length)
     * these additional pseudo-types are used for compressed encodings: */
    Compressionrlesmall = 7,
    /* start of large RLE run (8-bit length) */
    Compressionrlelarge = 8,
    /* same as the last COMPRESSION_SELF block */
    Compressionself0 = 9,
    /* same as the last COMPRESSION_SELF block + 1 */
    Compressionself1 = 10,
    /* same block in the parent */
    Compressionparentself = 11,
    /* same as the last COMPRESSION_PARENT block */
    Compressionparent0 = 12,
    /* same as the last COMPRESSION_PARENT block + 1 */
    Compressionparent1 = 13,

    /* ADDED HERE: used in CHD V3 and V4 */
    Compressionmini = 100,
    /* ADDED HERE: as an internal error state */
    Compressionerror = 101
}

public enum ChdError
{
    Chderrnone,
    Chderrnointerface,
    Chderroutofmemory,
    Chderrinvalidfile,
    Chderrinvalidparameter,
    Chderrinvaliddata,
    Chderrfilenotfound,
    Chderrrequiresparent,
    Chderrfilenotwriteable,
    Chderrreaderror,
    Chderrwriteerror,
    Chderrcodecerror,
    Chderrinvalidparent,
    Chderrhunkoutofrange,
    Chderrdecompressionerror,
    Chderrcompressionerror,
    Chderrcantcreatefile,
    Chderrcantverify,
    Chderrnotsupported,
    Chderrmetadatanotfound,
    Chderrinvalidmetadatasize,
    Chderrunsupportedversion,
    Chderrverifyincomplete,
    Chderrinvalidmetadata,
    Chderrinvalidstate,
    Chderroperationpending,
    Chderrnoasyncoperation,
    Chderrunsupportedformat,
    Chderrcannotopenfile
}
