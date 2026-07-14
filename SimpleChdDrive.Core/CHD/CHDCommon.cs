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
