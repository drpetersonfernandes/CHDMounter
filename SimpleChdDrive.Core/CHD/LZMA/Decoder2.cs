using SimpleChdDrive.Core.CHD.LZMA.RangeCoder;

namespace SimpleChdDrive.Core.CHD.LZMA;

internal struct Decoder2
{
    private BitDecoder[] _mDecoders;
    public void Create() { _mDecoders = new BitDecoder[0x300]; }
    public readonly void Init() { for (var i = 0; i < 0x300; i++) _mDecoders[i].Init(); }

    public readonly byte DecodeNormal(RangeCoder.Decoder rangeDecoder)
    {
        uint symbol = 1;
        do
        {
            symbol = (symbol << 1) | _mDecoders[symbol].Decode(rangeDecoder);
        } while (symbol < 0x100);
        return (byte)symbol;
    }

    public readonly byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, byte matchByte)
    {
        uint symbol = 1;
        do
        {
            var matchBit = (uint)(matchByte >> 7) & 1;
            matchByte <<= 1;
            var bit = _mDecoders[((1 + matchBit) << 8) + symbol].Decode(rangeDecoder);
            symbol = (symbol << 1) | bit;
            if (matchBit != bit)
            {
                while (symbol < 0x100)
                {
                    symbol = (symbol << 1) | _mDecoders[symbol].Decode(rangeDecoder);
                }
                break;
            }
        } while (symbol < 0x100);
        return (byte)symbol;
    }
}
