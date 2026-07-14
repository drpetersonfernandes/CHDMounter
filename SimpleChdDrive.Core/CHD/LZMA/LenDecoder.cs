using SimpleChdDrive.Core.CHD.LZMA.RangeCoder;

namespace SimpleChdDrive.Core.CHD.LZMA;

internal class LenDecoder
{
    private BitDecoder _mChoice;
    private BitDecoder _mChoice2;
    private readonly BitTreeDecoder[] _mLowCoder = new BitTreeDecoder[Base.KNumPosStatesMax];
    private readonly BitTreeDecoder[] _mMidCoder = new BitTreeDecoder[Base.KNumPosStatesMax];
    private readonly BitTreeDecoder _mHighCoder = new(Base.KNumHighLenBits);
    private uint _mNumPosStates;

    public void Create(uint numPosStates)
    {
        for (var posState = _mNumPosStates; posState < numPosStates; posState++)
        {
            _mLowCoder[posState] = new BitTreeDecoder(Base.KNumLowLenBits);
            _mMidCoder[posState] = new BitTreeDecoder(Base.KNumMidLenBits);
        }
        _mNumPosStates = numPosStates;
    }

    public void Init()
    {
        _mChoice.Init();
        for (uint posState = 0; posState < _mNumPosStates; posState++)
        {
            _mLowCoder[posState].Init();
            _mMidCoder[posState].Init();
        }
        _mChoice2.Init();
        _mHighCoder.Init();
    }

    public uint Decode(RangeCoder.Decoder rangeDecoder, uint posState)
    {
        if (_mChoice.Decode(rangeDecoder) == 0)
        {
            return _mLowCoder[posState].Decode(rangeDecoder);
        }
        else
        {
            var symbol = Base.KNumLowLenSymbols;
            if (_mChoice2.Decode(rangeDecoder) == 0)
            {
                symbol += _mMidCoder[posState].Decode(rangeDecoder);
            }
            else
            {
                symbol += Base.KNumMidLenSymbols;
                symbol += _mHighCoder.Decode(rangeDecoder);
            }
            return symbol;
        }
    }
}
