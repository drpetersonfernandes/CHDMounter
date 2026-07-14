using System.Security.Cryptography;
using System.Text;
using Serilog;
using SimpleChdDrive.Core.CHD.Utils;

namespace SimpleChdDrive.Core.CHD;

internal class ChdHeader
{
    public chdCodec[] Compression;
    public ChdReader[] ChdReader;

    public ulong Totalbytes;
    public uint Blocksize;
    public uint Totalblocks;

    // V5: size of a "unit" used for parent block address translation.
    // For V1-V4 parent entries the offset is a direct hunk index, so unitbytes
    // is only meaningful for V5 (set to blocksize as a harmless default otherwise).
    public uint Unitbytes;

    // True when the V5 map is the uncompressed variant. In that map an offset
    // word of 0 means "read this hunk from the parent" (or zero-fill if none).
    public bool UncompressedMap;

    public MapEntry[] Map;

    public byte[] Md5; // just compressed data
    public byte[] Rawsha1; // just compressed data
    public byte[] Sha1; // includes the meta data

    public byte[] Parentmd5;
    public byte[] Parentsha1;

    public ulong Metaoffset;
}

internal class MapEntry
{
    public CompressionType Comptype;
    public uint Length; // length of compressed data
    public ulong Offset; // offset of compressed data in file. Also index of source block for COMPRESSION_SELF
    public uint? Crc; // V3 & V4
    public ushort? Crc16; // V5

    public MapEntry SelfMapEntry; // link to self MapEntry data used in COMPRESSION_SELF (replaces offset index)

    //Used to optimmize block reading so that any block in only decompressed once.
    public int UseCount;

    public byte[] BuffIn;
    public byte[] BuffOutCache;
    public byte[] BuffOut;

    // Used in Parallel decompress to keep the blocks in order when hashing.
    public bool Processed;


    // Used to calculate which blocks should have buffered copies kept.
    public int UsageWeight;
    public bool KeepBufferCopy;
}

public static class Chd
{
    public const int TaskCount = 8;

    public static ChdError CheckFile(Stream s, string filename, bool deepCheck, out uint? chdVersion, out byte[] chdSha1, out byte[] chdMd5)
    {
        chdSha1 = null;
        chdMd5 = null;
        chdVersion = null;

        if (!CheckHeader(s, out var length, out var version))
            return ChdError.Chderrinvalidfile;

        Log.Information("CHD Version {Version}", version);
        var valid = ChdError.Chderrinvaliddata;
        ChdHeader chd = null;
        try
        {
            switch (version)
            {
                case 1:
                    valid = ChdHeaders.ReadHeaderV1(s, out chd);
                    break;
                case 2:
                    valid = ChdHeaders.ReadHeaderV2(s, out chd);
                    break;
                case 3:
                    valid = ChdHeaders.ReadHeaderV3(s, out chd);
                    break;
                case 4:
                    valid = ChdHeaders.ReadHeaderV4(s, out chd);
                    break;
                case 5:
                    valid = ChdHeaders.ReadHeaderV5(s, out chd);
                    break;
                default:
                    {
                        Log.Warning("Unknown version {Version}", version);
                        return ChdError.Chderrunsupportedversion;
                    }
            }
        }
        catch
        {
            valid = ChdError.Chderrinvaliddata;
        }

        if (valid != ChdError.Chderrnone)
        {
            Log.Warning("Child CHD found, cannot be processed");
            return valid;
        }

        if (chd != null)
        {
            chdSha1 = chd.Sha1 ?? chd.Rawsha1;
            chdMd5 = chd.Md5;
            chdVersion = version;

            if (!Util.IsAllZeroArray(chd.Parentmd5) || !Util.IsAllZeroArray(chd.Parentsha1))
            {
                Log.Warning("Child CHD found, cannot be processed");
                return ChdError.Chderrrequiresparent;
            }

            if (!deepCheck)
                return ChdError.Chderrnone;

            if (chd.Totalblocks * (ulong)chd.Blocksize != chd.Totalbytes)
            {
                Log.Debug("{BlocksXSize} != {TotalBytes}", chd.Totalblocks * (ulong)chd.Blocksize, chd.Totalbytes);
            }


            var strComp = "";
            foreach (var t in chd.Compression)
            {
                strComp += $", {t.ToString().Substring(10)}";
            }

            Log.Information("{Filename}, V:{Version} {Compression}", Path.GetFileName(filename), version, strComp);

            ChdBlockRead.FindBlockReaders(chd);
            ChdBlockRead.FindRepeatedBlocks(chd);
            var blocksToKeep = 1024 * 1024 * 512 / (int)chd.Blocksize;
            ChdBlockRead.KeepMostRepeatedBlocks(chd, blocksToKeep);

            valid = TaskCount == 0 ? DecompressData(s, chd) : DecompressDataParallel(s, chd);

            if (valid != ChdError.Chderrnone)
            {
                Log.Error("Data Decompress Failed: {Error}", valid);
                return valid;
            }

            valid = ChdMetaData.ReadMetaData(s, chd);
        }

        if (valid != ChdError.Chderrnone)
        {
            Log.Error("Meta Data Failed: {Error}", valid);
            return valid;
        }


        Log.Information("Valid");
        return ChdError.Chderrnone;
    }

    /// <summary>
    /// Fully verifies a (possibly child/differential) CHD, resolving parent
    /// references against the CHD at <paramref name="parentFilename"/> (pass null
    /// for a standalone CHD). Reads the whole image via the random-access
    /// <see cref="ChdFile"/> API, validates the raw-data hash (SHA1/MD5) and, for
    /// V4/V5, the metadata SHA1. This is a sequential (single-threaded) verify -
    /// use <see cref="CheckFile"/> for the fast parallel path on standalone CHDs.
    /// </summary>
    public static ChdError CheckFileWithParent(string filename, string parentFilename,
        out uint? chdVersion, out byte[] chdSha1, out byte[] chdMd5)
    {
        chdVersion = null;
        chdSha1 = null;
        chdMd5 = null;

        var err = ChdFile.Open(filename, parentFilename, out var chd);
        if (err != ChdError.Chderrnone)
            return err;

        using (chd)
        {
            chdVersion = chd.Version;
            chdSha1 = chd.Sha1;
            chdMd5 = chd.Md5;

            var expectedSha1 = chd.RawSha1;
            var expectedMd5 = chd.Md5;
            var haveSha1 = expectedSha1 != null && !Util.IsAllZeroArray(expectedSha1);
            var haveMd5 = expectedMd5 != null && !Util.IsAllZeroArray(expectedMd5);

            using var md5Check = haveMd5 ? MD5.Create() : null;
            using var sha1Check = haveSha1 ? SHA1.Create() : null;

            var buffer = new byte[chd.HunkBytes];
            var sizetoGo = chd.TotalBytes;
            ulong offset = 0;
            while (sizetoGo > 0)
            {
                var chunk = (int)Math.Min((ulong)buffer.Length, sizetoGo);
                err = chd.Read(offset, buffer, 0, chunk);
                if (err != ChdError.Chderrnone)
                    return err;

                md5Check?.TransformBlock(buffer, 0, chunk, null, 0);
                sha1Check?.TransformBlock(buffer, 0, chunk, null, 0);
                offset += (ulong)chunk;
                sizetoGo -= (ulong)chunk;
            }

            var tmp = Array.Empty<byte>();
            md5Check?.TransformFinalBlock(tmp, 0, 0);
            sha1Check?.TransformFinalBlock(tmp, 0, 0);

            if ((haveMd5 && !Util.ByteArrEquals(expectedMd5, md5Check.Hash)) || (haveSha1 && !Util.ByteArrEquals(expectedSha1, sha1Check.Hash)))
                return ChdError.Chderrdecompressionerror;

            return ChdError.Chderrnone;
        }
    }

    private static readonly uint[] HeaderLengths = [0, 76, 80, 120, 108, 124];
    private static readonly byte[] Id = "MComprHD"u8.ToArray();

    public static bool CheckHeader(Stream file, out uint length, out uint version)
    {
        foreach (var t in Id)
        {
            var b = (byte)file.ReadByte();
            if (b != t)
            {
                length = 0;
                version = 0;
                return false;
            }
        }

        using var br = new BinaryReader(file, Encoding.UTF8, true);
        length = br.ReadUInt32BE();
        version = br.ReadUInt32BE();
        return HeaderLengths[version] == length;
    }


    internal static ChdError DecompressData(Stream file, ChdHeader chd)
    {
        // stores the FLAC decompression classes for this instance.
        var codec = new ChdCodec();

        using var br = new BinaryReader(file, Encoding.UTF8, true);

        using var md5Check = chd.Md5 != null ? MD5.Create() : null;
        using var sha1Check = chd.Rawsha1 != null ? SHA1.Create() : null;

        var arrPool = new ArrayPool(chd.Blocksize);

        var buffer = new byte[chd.Blocksize];

        try
        {
            var block = 0;
            var sizetoGo = chd.Totalbytes;
            while (sizetoGo > 0)
            {
                /* progress */
                if (block % 1000 == 0)
                    Log.Debug("Verifying, {Percent:N1}% complete", 100 - sizetoGo * 100 / chd.Totalbytes);

                var mapEntry = chd.Map[block];
                if (mapEntry.Length > 0)
                {
                    mapEntry.BuffIn = arrPool.Rent();
                    file.Seek((long)mapEntry.Offset, SeekOrigin.Begin);
                    file.ReadExactly(mapEntry.BuffIn, 0, (int)mapEntry.Length);
                }

                /* read the block into the cache */
                var err = ChdBlockRead.ReadBlock(mapEntry, arrPool, chd.ChdReader, codec, buffer, (int)chd.Blocksize);
                if (err != ChdError.Chderrnone)
                    return err;

                if (mapEntry.Length > 0)
                {
                    arrPool.Return(mapEntry.BuffIn);
                    mapEntry.BuffIn = null;
                }

                var sizenext = sizetoGo > chd.Blocksize ? (int)chd.Blocksize : (int)sizetoGo;

                md5Check?.TransformBlock(buffer, 0, sizenext, null, 0);
                sha1Check?.TransformBlock(buffer, 0, sizenext, null, 0);

                /* prepare for the next block */
                block++;
                sizetoGo -= (ulong)sizenext;
            }
            Log.Debug("Verifying, 100.0% complete");
        }
        catch (Exception e)
        {
            Log.Error(e, "Data Decompress Failed");
            return ChdError.Chderrdecompressionerror;
        }

        var tmp = Array.Empty<byte>();
        md5Check?.TransformFinalBlock(tmp, 0, 0);
        sha1Check?.TransformFinalBlock(tmp, 0, 0);

        // here it is now using the rawsha1 value from the header to validate the raw binary data.
        if (chd.Md5 != null && !Util.IsAllZeroArray(chd.Md5) && md5Check != null && !Util.ByteArrEquals(chd.Md5, md5Check.Hash))
        {
            return ChdError.Chderrdecompressionerror;
        }
        if (chd.Rawsha1 != null && !Util.IsAllZeroArray(chd.Rawsha1) && sha1Check != null && !Util.ByteArrEquals(chd.Rawsha1, sha1Check.Hash))
        {
            return ChdError.Chderrdecompressionerror;
        }

        return ChdError.Chderrnone;
    }


    internal static ChdError DecompressDataParallel(Stream file, ChdHeader chd)
    {
        using var br = new BinaryReader(file, Encoding.UTF8, true);

        var md5Check = chd.Md5 != null ? MD5.Create() : null;
        var sha1Check = chd.Rawsha1 != null ? SHA1.Create() : null;

        var blocksToDecompress = new BlockingCollection<int>(TaskCount * 100);
        var blocksToHash = new BlockingCollection<int>(TaskCount * 100);
        var errMaster = ChdError.Chderrnone;

        var allTasks = new List<Task>();

        var ts = new CancellationTokenSource();
        var ct = ts.Token;

        var arrPoolIn = new ArrayPool(chd.Blocksize);
        var arrPoolOut = new ArrayPool(chd.Blocksize);
        var arrPoolCache = new ArrayPool(chd.Blocksize);

        var blocksToKeep = 1024 * 1024 * 512 / (int)chd.Blocksize;
        var aheadLock = new SemaphoreSlim(blocksToKeep, blocksToKeep);

        try
        {

        var producerThread = Task.Factory.StartNew(() =>
        {
            try
            {
                var blockPercent = chd.Totalblocks / 100;
                if (blockPercent == 0)
                {
                    blockPercent = 1;
                }

                for (var block = 0; block < chd.Totalblocks; block++)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    /* progress */
                    if (block % blockPercent == 0)
                    {
                        //arrPoolIn.ReadStats(out int issuedArraysTotalIn, out int returnedArraysTotalIn);
                        //arrPoolOut.ReadStats(out int issuedArraysTotalOut, out int returnedArraysTotalOut);
                        //arrPoolCache.ReadStats(out int issuedArraysTotalCache, out int returnedArraysTotalCache);
                        //progress?.Invoke($"Verifying: {(long)block * 100 / chd.totalblocks:N0}%     Load buffer: {blocksToDecompress.Count}   Hash buffer: {blocksToHash.Count}  {issuedArraysTotalIn},{returnedArraysTotalIn} | {issuedArraysTotalOut},{returnedArraysTotalOut} | {issuedArraysTotalCache},{returnedArraysTotalCache}\r");

                        //progress?.Invoke($"Verifying: {(long)block * 100 / chd.totalblocks:N0}%     Load buffer: {blocksToDecompress.Count}    Hash buffer: {blocksToHash.Count}");;

                        Log.Debug("Verifying: {Percent:N0}%", (long)block * 100 / chd.Totalblocks);
                    }
                    var mapEntry = chd.Map[block];

                    if (mapEntry.Length > 0)
                    {
                        if (file.Position != (long)mapEntry.Offset)
                            file.Seek((long)mapEntry.Offset, SeekOrigin.Begin);

                        mapEntry.BuffIn = arrPoolIn.Rent();
                        file.ReadExactly(mapEntry.BuffIn, 0, (int)mapEntry.Length);
                    }

                    blocksToDecompress.Add(block, ct);
                }
                // this must be done to tell all the decompression threads to stop working and return.
                for (var i = 0; i < TaskCount; i++)
                    blocksToDecompress.Add(-1);
            }
            catch
            {
                if (ct.IsCancellationRequested)
                    return;

                if (errMaster == ChdError.Chderrnone)
                {
                    errMaster = ChdError.Chderrinvalidfile;
                }

                ts.Cancel();
            }
        });
        allTasks.Add(producerThread);

        for (var i = 0; i < TaskCount; i++)
        {
            var decompressionThread = Task.Factory.StartNew(() =>
            {
                try
                {
                    var codec = new ChdCodec();
                    while (true)
                    {
                        aheadLock.Wait(ct);
                        var block = blocksToDecompress.Take(ct);
                        if (block == -1)
                            return;

                        var mapEntry = chd.Map[block];
                        mapEntry.BuffOut = arrPoolOut.Rent();
                        var err = ChdBlockRead.ReadBlock(mapEntry, arrPoolCache, chd.ChdReader, codec, mapEntry.BuffOut, (int)chd.Blocksize);
                        if (err != ChdError.Chderrnone)
                        {
                            ts.Cancel();
                            errMaster = err;
                            return;
                        }
                        blocksToHash.Add(block, ct);

                        if (mapEntry.Length > 0)
                        {
                            arrPoolIn.Return(mapEntry.BuffIn);
                            mapEntry.BuffIn = null;
                        }
                    }
                }
                catch
                {
                    if (ct.IsCancellationRequested)
                        return;

                    if (errMaster == ChdError.Chderrnone)
                    {
                        errMaster = ChdError.Chderrdecompressionerror;
                    }

                    ts.Cancel();
                }
            });

            allTasks.Add(decompressionThread);
        }

        var sizetoGo = chd.Totalbytes;
        var proc = 0;
        var hashingThread = Task.Factory.StartNew(() =>
        {
            try
            {
                while (true)
                {
                    var item = blocksToHash.Take(ct);

                    chd.Map[item].Processed = true;
                    while (chd.Map[proc].Processed)
                    {
                        var sizenext = sizetoGo > chd.Blocksize ? (int)chd.Blocksize : (int)sizetoGo;

                        var mapEntry = chd.Map[proc];

                        md5Check?.TransformBlock(mapEntry.BuffOut, 0, sizenext, null, 0);
                        sha1Check?.TransformBlock(mapEntry.BuffOut, 0, sizenext, null, 0);

                        arrPoolOut.Return(mapEntry.BuffOut);
                        mapEntry.BuffOut = null;
                        aheadLock.Release();

                        /* prepare for the next block */
                        sizetoGo -= (ulong)sizenext;

                        proc++;
                        if (proc == chd.Totalblocks)
                            return;
                    }
                }
            }
            catch
            {
                if (ct.IsCancellationRequested)
                    return;

                if (errMaster == ChdError.Chderrnone)
                {
                    errMaster = ChdError.Chderrdecompressionerror;
                }

                ts.Cancel();
            }
        });
        allTasks.Add(hashingThread);

        Task.WaitAll(allTasks.ToArray());


        Log.Debug("Verifying, 100% complete");

        arrPoolIn.ReadStats(out var issuedArraysTotal, out var returnedArraysTotal);
        Log.Debug("In: Issued Arrays Total {Issued}, returned Arrays Total {Returned}, block size {BlockSize}", issuedArraysTotal, returnedArraysTotal, chd.Blocksize);
        arrPoolOut.ReadStats(out issuedArraysTotal, out returnedArraysTotal);
        Log.Debug("Out: Issued Arrays Total {Issued}, returned Arrays Total {Returned}, block size {BlockSize}", issuedArraysTotal, returnedArraysTotal, chd.Blocksize);
        arrPoolCache.ReadStats(out issuedArraysTotal, out returnedArraysTotal);
        Log.Debug("Cache: Issued Arrays Total {Issued}, returned Arrays Total {Returned}, block size {BlockSize}", issuedArraysTotal, returnedArraysTotal, chd.Blocksize);

        if (errMaster != ChdError.Chderrnone)
            return errMaster;

        var tmp = Array.Empty<byte>();
        md5Check?.TransformFinalBlock(tmp, 0, 0);
        sha1Check?.TransformFinalBlock(tmp, 0, 0);

        // here it is now using the rawsha1 value from the header to validate the raw binary data.
        if (chd.Md5 != null && !Util.IsAllZeroArray(chd.Md5) && md5Check != null && !Util.ByteArrEquals(chd.Md5, md5Check.Hash))
        {
            return ChdError.Chderrdecompressionerror;
        }
        if (chd.Rawsha1 != null && !Util.IsAllZeroArray(chd.Rawsha1) && sha1Check != null && !Util.ByteArrEquals(chd.Rawsha1, sha1Check.Hash))
        {
            return ChdError.Chderrdecompressionerror;
        }

        return ChdError.Chderrnone;
        }
        finally
        {
            md5Check?.Dispose();
            sha1Check?.Dispose();
            blocksToDecompress?.Dispose();
            blocksToHash?.Dispose();
            ts.Dispose();
            aheadLock.Dispose();
        }
    }
}