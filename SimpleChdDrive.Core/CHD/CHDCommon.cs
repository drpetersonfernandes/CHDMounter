namespace SimpleChdDrive.Core.CHD;

internal static class ChdCommon
{
    internal static chdCodec CompTypeConv(uint ct)
    {
        switch (ct)
        {
            case 1:
            case 2: return chdCodec.Chdcodeczlib;
            case 3: return chdCodec.Chdcodecavhuff;
            default:
                return chdCodec.Chdcodecerror;
        }
    }

    /* Converts V3 & V4 mapFlags to V5 compression_type */
    internal static CompressionType ConvMapFlagstoCompressionType(MapFlags mapFlags)
    {
        switch (mapFlags & MapFlags.Mapentryflagtypemask)
        {
            case MapFlags.Mapentrytypeinvalid: return CompressionType.Compressionerror;
            case MapFlags.Mapentrytypecompressed: return CompressionType.Compressiontype0;
            case MapFlags.Mapentrytypeuncompressed: return CompressionType.Compressionnone;
            case MapFlags.Mapentrytypemini: return CompressionType.Compressionmini;
            case MapFlags.Mapentrytypeselfhunk: return CompressionType.Compressionself;
            case MapFlags.Mapentrytypeparenthunk: return CompressionType.Compressionparent;
            default:
                return CompressionType.Compressionerror;
        }
    }
}

public enum chdCodec
{
    Chdcodecnone = 0,
    Chdcodeczlib = 0x7A6C6962, // zlib
    Chdcodeclzma = 0x6C7A6D61, // lzma
    Chdcodechuffman = 0x68756666, // huff
    Chdcodecflac = 0x666C6163, // flac
    Chdcodeczstd = 0x7A737464, // zstd
    Chdcodeccdzlib = 0x63647A6C, // cdzl
    Chdcodeccdlzma = 0x63646C7A, // cdlz
    Chdcodeccdflac = 0x6364666C, // cdfl
    Chdcodeccdzstd = 0x63647A73, // cdzs
    Chdcodecavhuff = 0x61766875, // avhu
    Chdcodecerror = 0x0eeeeeee
}

[Flags]
public enum MapFlags
{
    Mapentryflagtypemask = 0x000f,      /* what type of hunk */
    Mapentryflagnocrc = 0x0010,         /* no CRC is present */

    Mapentrytypeinvalid = 0x0000,        /* invalid type */
    Mapentrytypecompressed = 0x0001,     /* standard compression */
    Mapentrytypeuncompressed = 0x0002,   /* uncompressed data */
    Mapentrytypemini = 0x0003,           /* mini: use offset as raw data */
    Mapentrytypeselfhunk = 0x0004,      /* same as another hunk in this file */
    Mapentrytypeparenthunk = 0x0005     /* same as a hunk in the parent file */
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
