namespace SimpleChdDrive.Core.CHD.LZMA;

internal class LiteralDecoder
{
    private Decoder2[] _mCoders = null!;
    private int _mNumPrevBits;
    private int _mNumPosBits;
    private uint _mPosMask;

    public void Create(int numPosBits, int numPrevBits)
    {
        unchecked
        {
            if (_mCoders != null && _mNumPrevBits == numPrevBits &&
                _mNumPosBits == numPosBits)
                return;

            _mNumPosBits = numPosBits;
            _mPosMask = ((uint)1 << numPosBits) - 1;
            _mNumPrevBits = numPrevBits;
            var numStates = (uint)1 << (_mNumPrevBits + _mNumPosBits);
            _mCoders = new Decoder2[numStates];
            for (uint i = 0; i < numStates; i++)
                _mCoders[i].Create();
        }
    }

    public void Init()
    {
        var numStates = (uint)1 << (_mNumPrevBits + _mNumPosBits);
        for (uint i = 0; i < numStates; i++)
            _mCoders[i].Init();
    }

    private uint GetState(uint pos, byte prevByte)
    { return ((pos & _mPosMask) << _mNumPrevBits) + (uint)(prevByte >> (8 - _mNumPrevBits)); }

    public byte DecodeNormal(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte)
    { return _mCoders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder); }

    public byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
    { return _mCoders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte); }
}
