namespace SimpleChdDrive.Core.CHD.LZMA;

internal class LenPriceTableEncoder : LenEncoder
{
    private readonly uint[] _prices = new uint[Base.KNumLenSymbols << Base.KNumPosStatesBitsEncodingMax];
    private uint _tableSize;
    private readonly uint[] _counters = new uint[Base.KNumPosStatesEncodingMax];

    public void SetTableSize(uint tableSize) { _tableSize = tableSize; }

    public uint GetPrice(uint symbol, uint posState)
    {
        return _prices[posState * Base.KNumLenSymbols + symbol];
    }

    private void UpdateTable(uint posState)
    {
        SetPrices(posState, _tableSize, _prices, posState * Base.KNumLenSymbols);
        _counters[posState] = _tableSize;
    }

    public void UpdateTables(uint numPosStates)
    {
        for (uint posState = 0; posState < numPosStates; posState++)
            UpdateTable(posState);
    }

    public new void Encode(RangeCoder.Encoder rangeEncoder, uint symbol, uint posState)
    {
        base.Encode(rangeEncoder, symbol, posState);
        if (--_counters[posState] == 0)
            UpdateTable(posState);
    }
}
