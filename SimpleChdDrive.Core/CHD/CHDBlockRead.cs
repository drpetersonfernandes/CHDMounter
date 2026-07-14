using Serilog;
using SimpleChdDrive.Core.CHD.Utils;

namespace SimpleChdDrive.Core.CHD;

internal static class ChdBlockRead
{
    // search for all COMPRESSION_SELF block, and increase the counter of the block it is referencing.
    // the first time the referenced block is decompressed a copy of its data is kept.
    // this copy is then used (instead of re-decompressing.) until the use count returns to zero
    // at which time the backup copy if removed.

    internal static void FindRepeatedBlocks(ChdHeader chd)
    {
        var totalFound = 0;
        var compressionCount = new int[5];
        var compressionSelfCount = new int[5];
        var compressionUniqueCount = new int[5];

        Parallel.ForEach(chd.Map, me =>
        {
            if (me.Comptype != CompressionType.Compressionself)
            {
                if ((int)me.Comptype < 5)
                    Interlocked.Increment(ref compressionCount[(int)me.Comptype]);
                return;
            }
            me.SelfMapEntry = chd.Map[me.Offset];
            switch (me.SelfMapEntry.Comptype)
            {
                case CompressionType.Compressiontype0:
                case CompressionType.Compressiontype1:
                case CompressionType.Compressiontype2:
                case CompressionType.Compressiontype3:
                case CompressionType.Compressionnone:
                    break;
                default:
                    Log.Error("Unexpected compression type {CompType}", me.SelfMapEntry.Comptype);
                    break;
            }

            lock (me.SelfMapEntry)
            {
                Interlocked.Increment(ref me.SelfMapEntry.UseCount);
                if (me.SelfMapEntry.UseCount == 1)
                    Interlocked.Increment(ref compressionUniqueCount[(int)me.SelfMapEntry.Comptype]);
            }

            Interlocked.Increment(ref compressionSelfCount[(int)me.SelfMapEntry.Comptype]);
            Interlocked.Increment(ref totalFound);
        });

        Log.Debug("Total Blocks {TotalBlocks}, Repeat Blocks {RepeatBlocks}, Output Block Size {BlockSize}", chd.Map.Length, totalFound, chd.Blocksize);
        for (var i = 0; i < 5; i++)
        {
            if ((compressionCount[i] == 0) & (compressionSelfCount[i] == 0))
                continue;

            var comp = "";
            if (i < chd.Compression.Length)
            {
                comp = chd.Compression[i].ToString();
            }
            else if (i == 4)
            {
                comp = "NONE";
            }

            Log.Debug("Compression {Index} : {Compression} : Block Count {Count}, Repeat Source Block Count {UniqueCount}, Repeat Total Block Count {SelfCount}", i, comp, compressionCount[i], compressionUniqueCount[i], compressionSelfCount[i]);
        }
    }

    internal static void KeepMostRepeatedBlocks(ChdHeader chd, int blocksToKeep)
    {
        var mapentries = new List<MapEntry>();
        foreach (var me in chd.Map)
        {
            if (me.UseCount > 0)
            {
                me.UsageWeight = GetWeigth(chd, me) * me.UseCount;
                mapentries.Add(me);
            }
        }

        Log.Debug("{Count} repeated used blocks", mapentries.Count);
        if (mapentries.Count < blocksToKeep)
            return;

        mapentries.Sort((a, b) => b.UsageWeight.CompareTo(a.UsageWeight));

        var c = 0;
        foreach (var me in mapentries)
        {
            if (c < blocksToKeep)
            {
                c++;
                me.KeepBufferCopy = true;
                continue;
            }

            me.KeepBufferCopy = false;
            me.UseCount = 0;
        }

        Parallel.ForEach(chd.Map, me =>
        {
            if (me.Comptype != CompressionType.Compressionself)
                return;
            // this should never be true
            if (me.SelfMapEntry == null)
                return;

            if (me.SelfMapEntry.KeepBufferCopy)
                return;

            me.Comptype = me.SelfMapEntry.Comptype;
            me.Length = me.SelfMapEntry.Length;
            me.Offset = me.SelfMapEntry.Offset;
            me.Crc = me.SelfMapEntry.Crc;
            me.Crc16 = me.SelfMapEntry.Crc16;
            me.SelfMapEntry = null!;
        });
    }
    internal static int GetWeigth(ChdHeader chd, MapEntry me)
    {
        if (me.Comptype == CompressionType.Compressionnone)
            return 1;

        switch (chd.Compression[(int)me.Comptype])
        {
            case ChdCodecType.Lzma: return 23;
            case ChdCodecType.Zlib: return 1;
            case ChdCodecType.Flac: return me.Length == 41 ? 1 : 2;
            case ChdCodecType.Huffman: return 64;

            case ChdCodecType.Avhu: return 1;

            case ChdCodecType.Cdfl: return me.Length == 15 ? 1 : 2;
            case ChdCodecType.Cdlz: return 18;
            case ChdCodecType.Cdzl: return 3;
            default: return 1;
        }
    }

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
                        var ret = ChdError.Chderrunsupportedformat;
                        ret = compression[(int)mapEntry.Comptype].Invoke(mapEntry.BuffIn, (int)mapEntry.Length, buffOut, buffOutLength, codec);

                        if (ret != ChdError.Chderrnone)
                            return ret;

                        // if this block is re-used keep a copy of it.
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
                // check CRC in the read_block_into_cache call
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