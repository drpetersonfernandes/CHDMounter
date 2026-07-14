using SimpleChdDrive.Core.CHD.Utils;

namespace SimpleChdDrive.Core.CHD;

internal static class ChdBlockRead
{
    internal static void FindBlockReaders(ChdHeader chd)
    {
        chd.ChdReader = new ChdReader[chd.Compression.Length];
        for (var i = 0; i < chd.Compression.Length; i++)
        {
            chd.ChdReader[i] = GetReaderFromCodec(chd.Compression[i]);
        }
    }

    private static ChdReader GetReaderFromCodec(ChdCodecType chdCodec)
    {
        switch (chdCodec)
        {
            case ChdCodecType.Zlib: return ChdReaders.Zlib;
            case ChdCodecType.Lzma: return ChdReaders.Lzma;
            case ChdCodecType.Huffman: return ChdReaders.Huffman;
            case ChdCodecType.Flac: return ChdReaders.Flac;
            case ChdCodecType.Zstd: return ChdReaders.Zstd;
            case ChdCodecType.Cdzl: return ChdReaders.Cdzlib;
            case ChdCodecType.Cdlz: return ChdReaders.Cdlzma;
            case ChdCodecType.Cdfl: return ChdReaders.Cdflac;
            case ChdCodecType.Cdzs: return ChdReaders.Cdzstd;
            case ChdCodecType.Avhu: return ChdReaders.AvHuff;
            default: return null!;
        }
    }

    internal static ChdError ReadBlock(MapEntry mapEntry, ArrayPool arrPool, ChdReader[] compression, ChdCodec codec, byte[] buffOut, int buffOutLength)
    {
        var checkCrc = true;

        switch (mapEntry.Comptype)
        {
            case CompressionType.Compressiontype0:
            case CompressionType.Compressiontype1:
            case CompressionType.Compressiontype2:
            case CompressionType.Compressiontype3:
            {
                lock (mapEntry)
                {
                    if (mapEntry.BuffOutCache == null)
                    {
                        var ret = compression[(int)mapEntry.Comptype].Invoke(mapEntry.BuffIn, (int)mapEntry.Length, buffOut, buffOutLength, codec);

                        if (ret != ChdError.Chderrnone)
                            return ret;

                        if (mapEntry.UseCount > 0)
                        {
                            mapEntry.BuffOutCache = arrPool.Rent();
                            Array.Copy(buffOut, 0, mapEntry.BuffOutCache, 0, buffOutLength);
                        }
                        break;
                    }

                    Array.Copy(mapEntry.BuffOutCache, 0, buffOut, 0, buffOutLength);

                    Interlocked.Decrement(ref mapEntry.UseCount);
                    if (mapEntry.UseCount == 0)
                    {
                        arrPool.Return(mapEntry.BuffOutCache);
                        mapEntry.BuffOutCache = null!;
                    }
                    checkCrc = false;
                }
                break;
            }
            case CompressionType.Compressionnone:
            {
                lock (mapEntry)
                {
                    if (mapEntry.BuffOutCache == null)
                    {
                        Array.Copy(mapEntry.BuffIn, 0, buffOut, 0, buffOutLength);

                        if (mapEntry.UseCount > 0)
                        {
                            mapEntry.BuffOutCache = arrPool.Rent();
                            Array.Copy(buffOut, 0, mapEntry.BuffOutCache, 0, buffOutLength);
                        }
                        break;
                    }


                    Array.Copy(mapEntry.BuffOutCache, 0, buffOut, 0, buffOutLength);
                    Interlocked.Decrement(ref mapEntry.UseCount);
                    if (mapEntry.UseCount == 0)
                    {
                        arrPool.Return(mapEntry.BuffOutCache);
                        mapEntry.BuffOutCache = null!;
                    }

                    checkCrc = false;
                }
                break;
            }

            case CompressionType.Compressionmini:
            {
                var tmp = BitConverter.GetBytes(mapEntry.Offset);
                for (var i = 0; i < 8; i++)
                {
                    buffOut[i] = tmp[7 - i];
                }

                for (var i = 8; i < buffOutLength; i++)
                {
                    buffOut[i] = buffOut[i - 8];
                }

                break;
            }

            case CompressionType.Compressionself:
            {
                var retcs = ReadBlock(mapEntry.SelfMapEntry, arrPool, compression, codec, buffOut, buffOutLength);
                if (retcs != ChdError.Chderrnone)
                    return retcs;
                checkCrc = false;
                break;
            }
            default:
                return ChdError.Chderrdecompressionerror;
        }

        if (checkCrc)
        {
            if ((mapEntry.Crc != null && !Crc.VerifyDigest((uint)mapEntry.Crc, buffOut, 0, (uint)buffOutLength)) || (mapEntry.Crc16 != null && Crc16.Calc(buffOut, buffOutLength) != mapEntry.Crc16))
                return ChdError.Chderrdecompressionerror;
        }
        return ChdError.Chderrnone;
    }
}
