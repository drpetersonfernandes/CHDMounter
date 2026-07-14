namespace SimpleChdDrive.Core.CHD.Utils;

internal class HuffmanDecoderRle : HuffmanDecoder
{
    private int _rlecount;
    private uint _prevdata;

    public HuffmanDecoderRle(uint numcodes, byte maxbits, BitStream bitbuf, ushort[] buffLookup) : base(numcodes, maxbits, bitbuf, buffLookup)
    { }

    public void Reset()
    {
        _rlecount = 0;
        _prevdata = 0;
    }
    public void FlushRle()
    {
        _rlecount = 0;
    }

    public new uint DecodeOne()
    {
        // return RLE data if we still have some
        if (_rlecount != 0)
        {
            _rlecount--;
            return _prevdata;
        }

        // fetch the data and process
        var data = base.DecodeOne();
        if (data < 0x100)
        {
            _prevdata += data;
            return _prevdata;
        }
        else
        {
            _rlecount = CodeToRleCount((int)data);
            _rlecount--;
            return _prevdata;
        }
    }

    public static int CodeToRleCount(int code)
    {
        return code switch
        {
            0x00 => 1,
            <= 0x107 => 8 + (code - 0x100),
            _ => 16 << (code - 0x108)
        };
    }
}
