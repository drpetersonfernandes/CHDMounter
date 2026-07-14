namespace SimpleChdDrive.Core.CHD.Utils;

internal class BitStream
{
    private uint _buffer;
    private int _bits;
    private readonly byte[] _readBuffer;
    private int _doffset;
    private readonly int _dlength;

    private readonly int _initialOffset;

    public bool Overflow()
    {
        return _doffset - _bits / 8 > _dlength;
    }

    /*-------------------------------------------------
    *  create_bitstream - constructor
    *-------------------------------------------------
    */
    public BitStream(byte[] src, int offset, int length)
    {
        _buffer = 0;
        _bits = 0;
        _readBuffer = src;
        _doffset = _initialOffset = offset;
        _dlength = offset + length;
    }

    /*-----------------------------------------------------
    *  bitstream_peek - fetch the requested number of bits
    *  but don't advance the input pointer
    *-----------------------------------------------------
    */
    public uint Peek(int numbits)
    {
        if (numbits == 0)
            return 0;

        /* fetch data if we need more */
        if (numbits > _bits)
        {
            while (_bits <= 24)
            {
                if (_doffset < _dlength)
                {
                    _buffer |= (uint)_readBuffer[_doffset] << (24 - _bits);
                }

                _doffset++;
                _bits += 8;
            }
        }

        /* return the data */
        return _buffer >> (32 - numbits);
    }

    /*-----------------------------------------------------
    *  bitstream_remove - advance the input pointer by the
    *  specified number of bits
    *-----------------------------------------------------
    */
    public void Remove(int numbits)
    {
        _buffer <<= numbits;
        _bits -= numbits;
    }


    /*-----------------------------------------------------
    *  bitstream_read - fetch the requested number of bits
    *-----------------------------------------------------
    */
    public uint Read(int numbits)
    {
        var result = Peek(numbits);
        Remove(numbits);
        return result;
    }

    /*-------------------------------------------------
    *  flush - flush to the nearest byte
    *-------------------------------------------------
    */

    public int Flush()
    {
        while (_bits >= 8)
        {
            _doffset--;
            _bits -= 8;
        }
        _bits = 0;
        _buffer = 0;
        return _doffset - _initialOffset;
    }
}
