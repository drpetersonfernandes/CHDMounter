using SimpleChdDrive.Core.CHD.Flac;
using SimpleChdDrive.Core.CHD.Flac.FlacDeps;
using ZstdSharp;

namespace SimpleChdDrive.Core.CHD;

internal class ChdCodec
{
    internal AudioPcmConfig FlacSettings = null!;
    internal AudioDecoder FlacAudioDecoder = null!;
    internal AudioBuffer FlacAudioBuffer = null!;


    internal AudioPcmConfig AvhuffSettings = null!;
    internal AudioDecoder AvhuffAudioDecoder = null!;


    internal byte[] BSector = null!;
    internal byte[] BSubcode = null!;

    internal byte[] Blzma = null!;

    internal Decompressor BZstd = null!;

    internal ushort[] BHuffman = null!;
    internal ushort[] BHuffmanHi = null!;
    internal ushort[] BHuffmanLo = null!;

    internal ushort[] BHuffmanY = null!;
    internal ushort[] BHuffmanCb = null!;
    internal ushort[] BHuffmanCr = null!;
}