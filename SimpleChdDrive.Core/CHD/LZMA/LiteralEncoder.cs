namespace SimpleChdDrive.Core.CHD.LZMA;

internal class LiteralEncoder
{
    private Encoder2[] _mCoders = null!;
    private int _mNumPrevBits;
    private int _mNumPosBits;
    private uint _mPosMask;

    public void Create(int numPosBits, int numPrevBits)
    {
        if (_mCoders != null && _mNumPrevBits == numPrevBits && _mNumPosBits == numPosBits)
            return;

        _mNumPosBits = numPosBits;
        _mPosMask = ((uint)1 << numPosBits) - 1;
        _mNumPrevBits = numPrevBits;
        var numStates = (uint)1 << (_mNumPrevBits + _mNumPosBits);
        _mCoders = new Encoder2[numStates];
        for (uint i = 0; i < numStates; i++)
            _mCoders[i].Create();
    }

    public void Init()
    {
        var numStates = (uint)1 << (_mNumPrevBits + _mNumPosBits);
        for (uint i = 0; i < numStates; i++)
            _mCoders[i].Init();
    }

    public Encoder2 GetSubCoder(uint pos, byte prevByte)
    { return _mCoders[((pos & _mPosMask) << _mNumPrevBits) + (uint)(prevByte >> (8 - _mNumPrevBits))]; }
}
