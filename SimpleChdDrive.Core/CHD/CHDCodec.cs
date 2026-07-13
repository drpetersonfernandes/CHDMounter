using SimpleChdDrive.Core.CHD.Flac;
using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD;

internal class ChdCodec
{
    internal AudioPCMConfig FlacSettings;
    internal AudioDecoder FlacAudioDecoder;
    internal AudioBuffer FlacAudioBuffer;


    internal AudioPCMConfig AvhuffSettings;
    internal AudioDecoder AvhuffAudioDecoder;


    internal byte[] BSector;
    internal byte[] BSubcode;

    internal byte[] Blzma;

    internal ZstdSharp.Decompressor BZstd;

    internal ushort[] BHuffman;
    internal ushort[] BHuffmanHi;
    internal ushort[] BHuffmanLo;

    internal ushort[] BHuffmanY;
    internal ushort[] BHuffmanCb;
    internal ushort[] BHuffmanCr;
}