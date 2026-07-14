using System.IO.Compression;
using SimpleChdDrive.Core.CHD.Flac;
using SimpleChdDrive.Core.CHD.Flac.FlacDeps;
using SimpleChdDrive.Core.CHD.LZMA;
using SimpleChdDrive.Core.CHD.Utils;
using ZstdSharp;

namespace SimpleChdDrive.Core.CHD;

internal delegate chd_error ChdReader(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec);

internal static partial class ChdReaders
{
    internal static chd_error Zlib(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        return Zlib(buffIn, 0, buffInLength, buffOut, buffOutLength);
    }
    private static chd_error Zlib(byte[] buffIn, int buffInStart, int buffInLength, byte[] buffOut, int buffOutLength)
    {
        using var memStream = new MemoryStream(buffIn, buffInStart, buffInLength, false);
        using var compStream = new DeflateStream(memStream, CompressionMode.Decompress, true);
        var bytesRead = 0;
        while (bytesRead < buffOutLength)
        {
            var bytes = compStream.Read(buffOut, bytesRead, buffOutLength - bytesRead);
            if (bytes == 0)
                return chd_error.CHDERR_INVALID_DATA;

            bytesRead += bytes;
        }
        return chd_error.CHDERR_NONE;
    }


    internal static chd_error Zstd(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        return Zstd(buffIn, 0, buffInLength, buffOut, 0, buffOutLength, codec);
    }
    private static chd_error Zstd(byte[] buffIn, int buffInStart, int buffInLength, byte[] buffOut, int buffOutStart, int buffOutLength, ChdCodec codec)
    {
        codec.BZstd ??= new Decompressor();
        try
        {
            var written = codec.BZstd.Unwrap(
                new ReadOnlySpan<byte>(buffIn, buffInStart, buffInLength),
                new Span<byte>(buffOut, buffOutStart, buffOutLength));
            if (written != buffOutLength)
                return chd_error.CHDERR_DECOMPRESSION_ERROR;
        }
        catch
        {
            return chd_error.CHDERR_DECOMPRESSION_ERROR;
        }
        return chd_error.CHDERR_NONE;
    }

    internal static chd_error Lzma(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        return Lzma(buffIn, 0, buffInLength, buffOut, buffOutLength, codec);
    }
    private static chd_error Lzma(byte[] buffIn, int buffInStart, int compsize, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        // CHD LZMA hunks are RAW, headerless LZMA payloads. There is no 5-byte
        // LZMA properties header stored in the stream (unlike a .lzma file).
        // Both MAME's chdman (encoder) and libchdr (decoder) use FIXED settings
        // and synthesise the properties rather than reading them:
        //   lc=3, lp=0, pb=2  =>  properties[0] = (pb*5 + lp)*9 + lc = 93  (== libchdr decoder_props[0])
        // The dictionary size only has to be >= the maximum back-reference
        // distance. Each hunk is compressed independently, so that distance is
        // always < hunkbytes; using buffOutLength (= hunkbytes) is therefore
        // always sufficient and keeps the reusable dictionary buffer small.
        // Do NOT try to read properties from the first bytes of buffIn - those
        // bytes are already compressed data and skipping them corrupts the hunk.
        var properties = new byte[5];
        const int posStateBits = 2;
        const int numLiteralPosStateBits = 0;
        const int numLiteralContextBits = 3;
        var dictionarySize = buffOutLength;
        properties[0] = (posStateBits * 5 + numLiteralPosStateBits) * 9 + numLiteralContextBits;
        for (var j = 0; j < 4; j++)
        {
            properties[1 + j] = (byte)((dictionarySize >> (8 * j)) & 0xFF);
        }

        if (codec.Blzma == null)
        {
            codec.Blzma = new byte[dictionarySize];
        }

        using var memStream = new MemoryStream(buffIn, buffInStart, compsize, false);
        using var compStream = new LzmaStream(properties, memStream, -1, -1, null, false, codec.Blzma);
        var bytesRead = 0;
        while (bytesRead < buffOutLength)
        {
            var bytes = compStream.Read(buffOut, bytesRead, buffOutLength - bytesRead);
            if (bytes == 0)
                return chd_error.CHDERR_INVALID_DATA;

            bytesRead += bytes;
        }

        return chd_error.CHDERR_NONE;
    }

    internal static chd_error Huffman(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        if (codec.BHuffman == null)
        {
            codec.BHuffman = new ushort[1 << 16];
        }

        var bitbuf = new BitStream(buffIn, 0, buffInLength);
        var hd = new HuffmanDecoder(256, 16, bitbuf, codec.BHuffman);

        if (hd.ImportTreeHuffman() != huffman_error.HUFFERR_NONE)
            return chd_error.CHDERR_INVALID_DATA;

        for (var j = 0; j < buffOutLength; j++)
        {
            buffOut[j] = (byte)hd.DecodeOne();
        }
        return chd_error.CHDERR_NONE;
    }

    internal static chd_error Flac(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        var endianType = buffIn[0];
        //CHD adds a leading char to indicate endian. Not part of the flac format.
        var swapEndian = endianType == 'B'; //'L'ittle / 'B'ig
        return Flac(buffIn, 1, buffInLength, buffOut, buffOutLength, swapEndian, codec, out _);
    }


    private static chd_error Flac(byte[] buffIn, int buffInStart, int buffInLength, byte[] buffOut, int buffOutLength, bool swapEndian, ChdCodec codec, out int srcPos)
    {
        // CHD FLAC data is HEADERLESS - it is a bare sequence of FLAC frames with
        // NO fLaC stream marker and NO STREAMINFO metadata block. There is nothing
        // to read the sample rate / channels / bit-depth from; that information is
        // implicit in the CHD format itself.
        //
        // libchdr does the same thing: flac_codec_decompress() hardcodes
        // flac_decoder_reset(..., 44100, 2, ...). The 44100 is arbitrary - sample
        // rate does NOT affect FLAC sample-value decoding (AudioDecoder only
        // validates that the per-frame rate code is a standard one and otherwise
        // ignores it). What matters for correct decoding is:
        //   - bits-per-sample = 16  (always true for CHD FLAC)
        //   - channel count   = 2   (CD/raw FLAC hunks are 16-bit stereo samples)
        // Both are fixed by the CHD format and validated against each frame header
        // inside DecodeFrame(); the actual per-frame block size is also read from
        // the frame header, so no block-size hint is required here.
        codec.FlacSettings ??= new AudioPcmConfig(16, 2, 44100);
        codec.FlacAudioDecoder ??= new AudioDecoder(codec.FlacSettings);
        codec.FlacAudioBuffer ??= new AudioBuffer(codec.FlacSettings, buffOutLength); //audio buffer to take decoded samples and read them to bytes.

        srcPos = buffInStart;
        var dstPos = 0;
        //this may require some error handling. Hopefully the while condition is reliable
        while (dstPos < buffOutLength)
        {
            var read = codec.FlacAudioDecoder.DecodeFrame(buffIn, srcPos, buffInLength - srcPos);
            codec.FlacAudioDecoder.Read(codec.FlacAudioBuffer, (int)codec.FlacAudioDecoder.Remaining);
            Array.Copy(codec.FlacAudioBuffer.Bytes, 0, buffOut, dstPos, codec.FlacAudioBuffer.ByteLength);
            dstPos += codec.FlacAudioBuffer.ByteLength;
            srcPos += read;
        }

        //Nanook - hack to support 16bit byte flipping - tested passes hunk CRC test
        if (swapEndian)
        {
            for (var i = 0; i < buffOutLength; i += 2)
            {
                (buffOut[i], buffOut[i + 1]) = (buffOut[i + 1], buffOut[i]);
            }
        }
        return chd_error.CHDERR_NONE;
    }

    /******************* CD decoders **************************/

    private const int CdMaxSectorData = 2352;
    private const int CdMaxSubcodeData = 96;
    private const int CdFrameSize = CdMaxSectorData + CdMaxSubcodeData;

    private static readonly byte[] SCdSyncHeader = [0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00];

    internal static chd_error Cdzlib(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        /* determine header bytes */
        var frames = buffOutLength / CdFrameSize;
        var complenBytes = buffOutLength < 65536 ? 2 : 3;
        var eccBytes = (frames + 7) / 8;
        var headerBytes = eccBytes + complenBytes;

        /* extract compressed length of base */
        var complenBase = (buffIn[eccBytes + 0] << 8) | buffIn[eccBytes + 1];
        if (complenBytes > 2)
        {
            complenBase = (complenBase << 8) | buffIn[eccBytes + 2];
        }

        codec.BSector ??= new byte[frames * CdMaxSectorData];
        codec.BSubcode ??= new byte[frames * CdMaxSubcodeData];

        var err = Zlib(buffIn, headerBytes, complenBase, codec.BSector, frames * CdMaxSectorData);
        if (err != chd_error.CHDERR_NONE)
            return err;

        err = Zlib(buffIn, headerBytes + complenBase, buffInLength - headerBytes - complenBase, codec.BSubcode, frames * CdMaxSubcodeData);
        if (err != chd_error.CHDERR_NONE)
            return err;

        /* reassemble the data */
        for (var framenum = 0; framenum < frames; framenum++)
        {
            Array.Copy(codec.BSector, framenum * CdMaxSectorData, buffOut, framenum * CdFrameSize, CdMaxSectorData);
            Array.Copy(codec.BSubcode, framenum * CdMaxSubcodeData, buffOut, framenum * CdFrameSize + CdMaxSectorData, CdMaxSubcodeData);

            // reconstitute the ECC data and sync header
            var sectorStart = framenum * CdFrameSize;
            if ((buffIn[framenum / 8] & (1 << (framenum % 8))) != 0)
            {
                Array.Copy(SCdSyncHeader, 0, buffOut, sectorStart, SCdSyncHeader.Length);
                cdRom.ecc_generate(buffOut, sectorStart);
            }
        }
        return chd_error.CHDERR_NONE;
    }


    internal static chd_error Cdlzma(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        /* determine header bytes */
        var frames = buffOutLength / CdFrameSize;
        var complenBytes = buffOutLength < 65536 ? 2 : 3;
        var eccBytes = (frames + 7) / 8;
        var headerBytes = eccBytes + complenBytes;

        /* extract compressed length of base */
        var complenBase = (buffIn[eccBytes + 0] << 8) | buffIn[eccBytes + 1];
        if (complenBytes > 2)
        {
            complenBase = (complenBase << 8) | buffIn[eccBytes + 2];
        }

        codec.BSector ??= new byte[frames * CdMaxSectorData];
        codec.BSubcode ??= new byte[frames * CdMaxSubcodeData];

        var err = Lzma(buffIn, headerBytes, complenBase, codec.BSector, frames * CdMaxSectorData, codec);
        if (err != chd_error.CHDERR_NONE)
            return err;

        err = Zlib(buffIn, headerBytes + complenBase, buffInLength - headerBytes - complenBase, codec.BSubcode, frames * CdMaxSubcodeData);
        if (err != chd_error.CHDERR_NONE)
            return err;

        /* reassemble the data */
        for (var framenum = 0; framenum < frames; framenum++)
        {
            Array.Copy(codec.BSector, framenum * CdMaxSectorData, buffOut, framenum * CdFrameSize, CdMaxSectorData);
            Array.Copy(codec.BSubcode, framenum * CdMaxSubcodeData, buffOut, framenum * CdFrameSize + CdMaxSectorData, CdMaxSubcodeData);

            // reconstitute the ECC data and sync header
            var sectorStart = framenum * CdFrameSize;
            if ((buffIn[framenum / 8] & (1 << (framenum % 8))) != 0)
            {
                Array.Copy(SCdSyncHeader, 0, buffOut, sectorStart, SCdSyncHeader.Length);
                cdRom.ecc_generate(buffOut, sectorStart);
            }
        }
        return chd_error.CHDERR_NONE;
    }


    internal static chd_error Cdflac(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        var frames = buffOutLength / CdFrameSize;

        codec.BSector ??= new byte[frames * CdMaxSectorData];
        codec.BSubcode ??= new byte[frames * CdMaxSubcodeData];

        var err = Flac(buffIn, 0, buffInLength, codec.BSector, frames * CdMaxSectorData, true, codec, out var pos);
        if (err != chd_error.CHDERR_NONE)
            return err;

        err = Zlib(buffIn, pos, buffInLength - pos, codec.BSubcode, frames * CdMaxSubcodeData);
        if (err != chd_error.CHDERR_NONE)
            return err;

        /* reassemble the data */
        for (var framenum = 0; framenum < frames; framenum++)
        {
            Array.Copy(codec.BSector, framenum * CdMaxSectorData, buffOut, framenum * CdFrameSize, CdMaxSectorData);
            Array.Copy(codec.BSubcode, framenum * CdMaxSubcodeData, buffOut, framenum * CdFrameSize + CdMaxSectorData, CdMaxSubcodeData);
        }
        return chd_error.CHDERR_NONE;
    }


    internal static chd_error Cdzstd(byte[] buffIn, int buffInLength, byte[] buffOut, int buffOutLength, ChdCodec codec)
    {
        /* determine header bytes */
        var frames = buffOutLength / CdFrameSize;
        var complenBytes = buffOutLength < 65536 ? 2 : 3;
        var eccBytes = (frames + 7) / 8;
        var headerBytes = eccBytes + complenBytes;

        /* extract compressed length of base */
        var complenBase = (buffIn[eccBytes + 0] << 8) | buffIn[eccBytes + 1];
        if (complenBytes > 2)
        {
            complenBase = (complenBase << 8) | buffIn[eccBytes + 2];
        }

        codec.BSector ??= new byte[frames * CdMaxSectorData];
        codec.BSubcode ??= new byte[frames * CdMaxSubcodeData];

        var err = Zstd(buffIn, headerBytes, complenBase, codec.BSector, 0, frames * CdMaxSectorData, codec);
        if (err != chd_error.CHDERR_NONE)
            return err;

        err = Zstd(buffIn, headerBytes + complenBase, buffInLength - headerBytes - complenBase, codec.BSubcode, 0, frames * CdMaxSubcodeData, codec);
        if (err != chd_error.CHDERR_NONE)
            return err;

        /* reassemble the data */
        for (var framenum = 0; framenum < frames; framenum++)
        {
            Array.Copy(codec.BSector, framenum * CdMaxSectorData, buffOut, framenum * CdFrameSize, CdMaxSectorData);
            Array.Copy(codec.BSubcode, framenum * CdMaxSubcodeData, buffOut, framenum * CdFrameSize + CdMaxSectorData, CdMaxSubcodeData);

            // reconstitute the ECC data and sync header
            var sectorStart = framenum * CdFrameSize;
            if ((buffIn[framenum / 8] & (1 << (framenum % 8))) != 0)
            {
                Array.Copy(SCdSyncHeader, 0, buffOut, sectorStart, SCdSyncHeader.Length);
                cdRom.ecc_generate(buffOut, sectorStart);
            }
        }
        return chd_error.CHDERR_NONE;
    }
}