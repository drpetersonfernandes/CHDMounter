using SimpleChdDrive.Core.CHD.LZMA.RangeCoder;

namespace SimpleChdDrive.Core.CHD.LZMA;

internal struct Encoder2
{
    private BitEncoder[] _mEncoders;

    public void Create() { _mEncoders = new BitEncoder[0x300]; }

    public readonly void Init() { for (var i = 0; i < 0x300; i++) _mEncoders[i].Init(); }

    public readonly void Encode(RangeCoder.Encoder rangeEncoder, byte symbol)
    {
        uint context = 1;
        for (var i = 7; i >= 0; i--)
        {
            var bit = (uint)((symbol >> i) & 1);
            _mEncoders[context].Encode(rangeEncoder, bit);
            context = (context << 1) | bit;
        }
    }

    public readonly void EncodeMatched(RangeCoder.Encoder rangeEncoder, byte matchByte, byte symbol)
    {
        uint context = 1;
        var same = true;
        for (var i = 7; i >= 0; i--)
        {
            var bit = (uint)((symbol >> i) & 1);
            var state = context;
            if (same)
            {
                var matchBit = (uint)((matchByte >> i) & 1);
                state += (1 + matchBit) << 8;
                same = matchBit == bit;
            }
            _mEncoders[state].Encode(rangeEncoder, bit);
            context = (context << 1) | bit;
        }
    }

    public uint GetPrice(bool matchMode, byte matchByte, byte symbol)
    {
        uint price = 0;
        uint context = 1;
        var i = 7;
        if (matchMode)
        {
            for (; i >= 0; i--)
            {
                var matchBit = (uint)(matchByte >> i) & 1;
                var bit = (uint)(symbol >> i) & 1;
                price += _mEncoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                context = (context << 1) | bit;
                if (matchBit != bit)
                {
                    i--;
                    break;
                }
            }
        }
        for (; i >= 0; i--)
        {
            var bit = (uint)(symbol >> i) & 1;
            price += _mEncoders[context].GetPrice(bit);
            context = (context << 1) | bit;
        }
        return price;
    }
}
