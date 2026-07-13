using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public unsafe class BitReader
{
    #region Static Methods

    public static int Log2I(int v)
    {
        return Log2I((uint)v);
    }

    public static readonly byte[] MultiplyDeBruijnBitPosition =
    [
        0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
        8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
    ];

    public static int Log2I(ulong v)
    {
        v |= v >> 1; // first round down to one less than a power of 2
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        if (v >> 32 == 0)
            return MultiplyDeBruijnBitPosition[((uint)v * 0x07C4ACDDU) >> 27];

        return 32 + MultiplyDeBruijnBitPosition[((uint)(v >> 32) * 0x07C4ACDDU) >> 27];
    }

    public static int Log2I(uint v)
    {
        v |= v >> 1; // first round down to one less than a power of 2
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        return MultiplyDeBruijnBitPosition[(v * 0x07C4ACDDU) >> 27];
    }

    public static readonly byte[] ByteToUnaryTable =
    [
        8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    ];

    #endregion

    private byte* _bptrM;
    private int _bufferLenM;
    private int _haveBitsM;
    private ulong _cacheM;
    private ushort _crc16M;

    public int Position => (int)(_bptrM - Buffer - (_haveBitsM >> 3));

    public byte* Buffer { get; private set; }

    public BitReader()
    {
        Buffer = null;
        _bptrM = null;
        _bufferLenM = 0;
        _haveBitsM = 0;
        _cacheM = 0;
        _crc16M = 0;
    }

    public BitReader(byte* buffer, int pos, int len)
    {
        Reset(buffer, pos, len);
    }

    public void Reset(byte* buffer, int pos, int len)
    {
        Buffer = buffer;
        _bptrM = buffer + pos;
        _bufferLenM = len;
        _haveBitsM = 0;
        _cacheM = 0;
        _crc16M = 0;
        Fill();
    }

    public void Fill()
    {
        while (_haveBitsM < 56)
        {
            _haveBitsM += 8;
            var b = *_bptrM++;
            _cacheM |= (ulong)b << (64 - _haveBitsM);
            _crc16M = (ushort)((_crc16M << 8) ^ Crc16.table[(_crc16M >> 8) ^ b]);
        }
    }

    /* skip any number of bits */
    public void Skipbits(int bits)
    {
        while (bits > _haveBitsM)
        {
            bits -= _haveBitsM;
            _cacheM = 0;
            _haveBitsM = 0;
            Fill();
        }
        _cacheM <<= bits;
        _haveBitsM -= bits;
    }

    public long read_long()
    {
        return ((long)Readbits(32) << 32) | Readbits(32);
    }

    public ulong read_ulong()
    {
        return ((ulong)Readbits(32) << 32) | Readbits(32);
    }

    public int read_int()
    {
        return (int)Readbits(sizeof(int));
    }

    public uint read_uint()
    {
        return Readbits(sizeof(uint));
    }

    public short read_short()
    {
        return (short)Readbits(16);
    }

    public ushort read_ushort()
    {
        return (ushort)Readbits(16);
    }

    /* supports reading 1 to 32 bits, in big endian format */
    public uint Readbits(int bits)
    {
        Fill();
        var result = (uint)(_cacheM >> (64 - bits));
        Skipbits(bits);
        return result;
    }

    /* supports reading 1 to 64 bits, in big endian format */
    public ulong Readbits64(int bits)
    {
        if (bits <= 56)
            return Readbits(bits);

        return ((ulong)Readbits(32) << (bits - 32)) | Readbits(bits - 32);
    }

    /* reads a single bit */
    public uint Readbit()
    {
        return Readbits(1);
    }

    public uint read_unary()
    {
        Fill();
        uint val = 0;
        var result = _cacheM >> 56;
        while (result == 0)
        {
            val += 8;
            _cacheM <<= 8;
            var b = *_bptrM++;
            _cacheM |= (ulong)b << (64 - _haveBitsM);
            _crc16M = (ushort)((_crc16M << 8) ^ Crc16.table[(_crc16M >> 8) ^ b]);
            result = _cacheM >> 56;
        }
        val += ByteToUnaryTable[result];
        Skipbits((int)(val & 7) + 1);
        return val;
    }

    public void Flush()
    {
        if ((_haveBitsM & 7) > 0)
        {
            _cacheM <<= _haveBitsM & 7;
            _haveBitsM -= _haveBitsM & 7;
        }
    }

    public ushort get_crc16()
    {
        if (_haveBitsM == 0)
            return _crc16M;

        ushort crc = 0;
        var n = _haveBitsM >> 3;
        for (var i = 0; i < n; i++)
        {
            crc = (ushort)((crc << 8) ^ Crc16.table[(crc >> 8) ^ (byte)(_cacheM >> (56 - (i << 3)))]);
        }

        return Crc16.Subtract(_crc16M, crc, n);
    }

    public int readbits_signed(int bits)
    {
        var val = (int)Readbits(bits);
        val <<= 32 - bits;
        val >>= 32 - bits;
        return val;
    }

    public uint read_utf8()
    {
        var x = Readbits(8);
        uint v;
        int i;
        if (0 == (x & 0x80))
        {
            v = x;
            i = 0;
        }
        else if (0xC0 == (x & 0xE0)) /* 110xxxxx */
        {
            v = x & 0x1F;
            i = 1;
        }
        else if (0xE0 == (x & 0xF0)) /* 1110xxxx */
        {
            v = x & 0x0F;
            i = 2;
        }
        else if (0xF0 == (x & 0xF8)) /* 11110xxx */
        {
            v = x & 0x07;
            i = 3;
        }
        else if (0xF8 == (x & 0xFC)) /* 111110xx */
        {
            v = x & 0x03;
            i = 4;
        }
        else if (0xFC == (x & 0xFE)) /* 1111110x */
        {
            v = x & 0x01;
            i = 5;
        }
        else if (0xFE == x) /* 11111110 */
        {
            v = 0;
            i = 6;
        }
        else
        {
            throw new Exception("invalid utf8 encoding");
        }
        for (; i > 0; i--)
        {
            x = Readbits(8);
            if (0x80 != (x & 0xC0))  /* 10xxxxxx */
                throw new Exception("invalid utf8 encoding");

            v <<= 6;
            v |= x & 0x3F;
        }
        return v;
    }

    public void read_rice_block(int n, int k, int* r)
    {
        Fill();
        fixed (byte* unaryTable = ByteToUnaryTable)
        fixed (ushort* t = Crc16.table)
        {
            var mask = (1U << k) - 1;
            var bptr = _bptrM;
            var haveBits = _haveBitsM;
            var cache = _cacheM;
            var crc = _crc16M;
            for (var i = n; i > 0; i--)
            {
                uint bits;
                var origBptr = bptr;
                while ((bits = unaryTable[cache >> 56]) == 8)
                {
                    cache <<= 8;
                    var b = *bptr++;
                    cache |= (ulong)b << (64 - haveBits);
                    crc = (ushort)((crc << 8) ^ t[(crc >> 8) ^ b]);
                }
                var msbs = bits + ((uint)(bptr - origBptr) << 3);
                // assumes k <= 41 (have_bits < 41 + 7 + 1 + 8 == 57, so we don't loose bits here)
                while (haveBits < 56)
                {
                    haveBits += 8;
                    var b = *bptr++;
                    cache |= (ulong)b << (64 - haveBits);
                    crc = (ushort)((crc << 8) ^ t[(crc >> 8) ^ b]);
                }

                var btsk = k + (int)bits + 1;
                var uval = (msbs << k) | (uint)((cache >> (64 - btsk)) & mask);
                cache <<= btsk;
                haveBits -= btsk;
                *r++ = (int)((uval >> 1) ^ -(int)(uval & 1));
            }
            _haveBitsM = haveBits;
            _cacheM = cache;
            _bptrM = bptr;
            _crc16M = crc;
        }
    }
}