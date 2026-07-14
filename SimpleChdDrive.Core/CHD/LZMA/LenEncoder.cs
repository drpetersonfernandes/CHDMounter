using SimpleChdDrive.Core.CHD.LZMA.RangeCoder;

namespace SimpleChdDrive.Core.CHD.LZMA;

internal class LenEncoder
{
    private BitEncoder _choice;
    private BitEncoder _choice2;
    private readonly BitTreeEncoder[] _lowCoder = new BitTreeEncoder[Base.KNumPosStatesEncodingMax];
    private readonly BitTreeEncoder[] _midCoder = new BitTreeEncoder[Base.KNumPosStatesEncodingMax];
    private readonly BitTreeEncoder _highCoder = new(Base.KNumHighLenBits);

    protected LenEncoder()
    {
        for (uint posState = 0; posState < Base.KNumPosStatesEncodingMax; posState++)
        {
            _lowCoder[posState] = new BitTreeEncoder(Base.KNumLowLenBits);
            _midCoder[posState] = new BitTreeEncoder(Base.KNumMidLenBits);
        }
    }

    public void Init(uint numPosStates)
    {
        _choice.Init();
        _choice2.Init();
        for (uint posState = 0; posState < numPosStates; posState++)
        {
            _lowCoder[posState].Init();
            _midCoder[posState].Init();
        }
        _highCoder.Init();
    }

    protected void Encode(RangeCoder.Encoder rangeEncoder, uint symbol, uint posState)
    {
        if (symbol < Base.KNumLowLenSymbols)
        {
            _choice.Encode(rangeEncoder, 0);
            _lowCoder[posState].Encode(rangeEncoder, symbol);
        }
        else
        {
            symbol -= Base.KNumLowLenSymbols;
            _choice.Encode(rangeEncoder, 1);
            if (symbol < Base.KNumMidLenSymbols)
            {
                _choice2.Encode(rangeEncoder, 0);
                _midCoder[posState].Encode(rangeEncoder, symbol);
            }
            else
            {
                _choice2.Encode(rangeEncoder, 1);
                _highCoder.Encode(rangeEncoder, symbol - Base.KNumMidLenSymbols);
            }
        }
    }

    protected void SetPrices(uint posState, uint numSymbols, uint[] prices, uint st)
    {
        var a0 = _choice.GetPrice0();
        var a1 = _choice.GetPrice1();
        var b0 = a1 + _choice2.GetPrice0();
        var b1 = a1 + _choice2.GetPrice1();
        uint i;
        for (i = 0; i < Base.KNumLowLenSymbols; i++)
        {
            if (i >= numSymbols)
                return;

            prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
        }
        for (; i < Base.KNumLowLenSymbols + Base.KNumMidLenSymbols; i++)
        {
            if (i >= numSymbols)
                return;

            prices[st + i] = b0 + _midCoder[posState].GetPrice(i - Base.KNumLowLenSymbols);
        }
        for (; i < numSymbols; i++)
        {
            prices[st + i] = b1 + _highCoder.GetPrice(i - Base.KNumLowLenSymbols - Base.KNumMidLenSymbols);
        }
    }
}
