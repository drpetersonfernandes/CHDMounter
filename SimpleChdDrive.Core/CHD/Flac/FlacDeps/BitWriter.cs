namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public abstract class BitWriter
{
    private ushort _crc16M;
    private ulong _bitBufM;
    private int _bitLeftM;
    private readonly int _bufStart;
    private int _bufPtrM;
    private readonly int _bufEnd;
    private bool _eof;

    public byte[] Buffer { get; }

    public int Length
    {
        get => _bufPtrM - _bufStart;
        set
        {
            Flush();
            _bufPtrM = _bufStart + value;
        }
    }

    public int BitLength => _bufPtrM * 8 + 64 - _bitLeftM;

    public ushort get_crc16()
    {
        return _crc16M;
    }

    protected BitWriter(byte[] buf, int pos, int len)
    {
        Buffer = buf;
        _bufStart = pos;
        _bufPtrM = pos;
        _bufEnd = pos + len;
        _bitLeftM = 64;
        _bitBufM = 0;
        _crc16M = 0;
        _eof = false;
    }

    public void Reset()
    {
        _bufPtrM = _bufStart;
        _bitLeftM = 64;
        _bitBufM = 0;
        _crc16M = 0;
        _eof = false;
    }

    public void Writebytes(int bytes, byte c)
    {
        for (; bytes > 0; bytes--)
        {
            Writebits(8, c);
        }
    }

    public unsafe void Writeints(int len, int pos, byte* buf)
    {
        var oldPos = BitLength;
        var start = oldPos / 8;
        var start1 = pos / 8;
        var end = (oldPos + len) / 8;
        var end1 = (pos + len) / 8;
        Flush();
        var startVal = oldPos % 8 != 0 ? Buffer[start] : (byte)0;
        fixed (byte* buf1 = &Buffer[0])
        {
            if (oldPos % 8 != 0)
            {
                _crc16M = Crc16.Subtract(_crc16M, 0, 1);
            }

            _crc16M = Crc16.ComputeChecksum(_crc16M, buf + start1, end - start);
            AudioSamples.MemCpy(buf1 + start, buf + start1, end - start);
            buf1[start] |= startVal;
        }
        _bufPtrM = end;
        if ((oldPos + len) % 8 != 0)
            Writebits((oldPos + len) % 8, buf[end1] >> (8 - (oldPos + len) % 8));
    }

    public void Write(params char[] chars)
    {
        foreach (var c in chars)
            Writebits(8, (byte)c);
    }

    public void Write(string s)
    {
        foreach (var t in s)
            Writebits(8, (byte)t);
    }

    public void Write(IEnumerable<byte> s)
    {
        foreach (var t in s)
            Writebits(8, t);
    }

    public void writebits_signed(int bits, int val)
    {
        Writebits(bits, val & ((1 << bits) - 1));
    }

    public void writebits_signed(uint bits, int val)
    {
        Writebits((int)bits, val & ((1 << (int)bits) - 1));
    }

    public void Writebits(int bits, int val)
    {
        Writebits(bits, (ulong)val);
    }

    public void Writebits(DateTime val)
    {
        var span = val.ToUniversalTime() - new DateTime(1904, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        Writebits(32, (ulong)span.TotalSeconds);
    }

    public void Writebits(int bits, uint val)
    {
        Writebits(bits, (ulong)val);
    }

    public void Writebits(int bits, ulong val)
    {
        //assert(bits == 32 || val < (1U << bits));

        if (bits == 0 || _eof) return;

        if (bits < _bitLeftM)
        {
            _bitLeftM -= bits;
            _bitBufM |= val << _bitLeftM;
        }
        else
        {
            var bb = _bitBufM | (val >> (bits - _bitLeftM));
            if (Buffer != null)
            {
                if (_bufPtrM + 8 > _bufEnd)
                {
                    _eof = true;
                    return;
                }

                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 56)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 48)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 40)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 32)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 24)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 16)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)(bb >> 8)]);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ (byte)bb]);

                Buffer[_bufPtrM + 7] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 6] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 5] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 4] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 3] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 2] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 1] = (byte)(bb & 0xFF);
                bb >>= 8;
                Buffer[_bufPtrM + 0] = (byte)(bb & 0xFF);
                _bufPtrM += 8;
            }
            // cannot do this in one shift, because bit_left_m can be 64,
            // 
            _bitLeftM += 64 - bits;
            _bitBufM = _bitLeftM == 64 ? 0 : val << _bitLeftM;
        }
    }

    /// <summary>
    /// Assumes there's enough space, buffer != null and bits is in range 1..31
    /// </summary>
    /// <param name="val"></param>
    //        unsafe void writebits_fast(int bits, uint val, ref byte* buf)
    //        {
    //#if DEBUG
    //            if ((buf_ptr + 3) >= buf_end)
    //            {
    //                eof = true;
    //                return;
    //            }
    //#endif
    //            if (bits < bit_left)
    //            {
    //                bit_buf = (bit_buf << bits) | val;
    //                bit_left -= bits;
    //            }
    //            else
    //            {
    //                uint bb = (bit_buf << bit_left) | (val >> (bits - bit_left));
    //                bit_left += (32 - bits);

    //                *(buf++) = (byte)(bb >> 24);
    //                *(buf++) = (byte)(bb >> 16);
    //                *(buf++) = (byte)(bb >> 8);
    //                *(buf++) = (byte)(bb);

    //                bit_buf = val;
    //            }
    //        }
    public void write_utf8(int val)
    {
        write_utf8((uint)val);
    }

    public void write_utf8(uint val)
    {
        if (val < 0x80)
        {
            Writebits(8, val);
            return;
        }
        var bytes = (BitReader.Log2I(val) + 4) / 5;
        var shift = (bytes - 1) * 6;
        Writebits(8, (256U - (256U >> bytes)) | (val >> shift));
        while (shift >= 6)
        {
            shift -= 6;
            Writebits(8, 0x80 | ((val >> shift) & 0x3F));
        }
    }

    public void write_unary_signed(int val)
    {
        // convert signed to unsigned
        var v = -2 * val - 1;
        v ^= v >> 31;

        // write quotient in unary
        var q = v + 1;
        while (q > 31)
        {
            Writebits(31, 0);
            q -= 31;
        }
        Writebits(q, 1);
    }

    public void write_rice_signed(int k, int val)
    {
        // convert signed to unsigned
        var v = -2 * val - 1;
        v ^= v >> 31;

        // write quotient in unary
        var q = (v >> k) + 1;
        while (q + k > 31)
        {
            var b = Math.Min(q + k - 31, 31);
            Writebits(b, 0);
            q -= b;
        }

        // write remainder in binary using 'k' bits
        Writebits(k + q, (v & ((1 << k) - 1)) | (1 << k));
    }

    public unsafe void write_rice_block_signed(byte* fixedbuf, int k, int* residual, int count)
    {
        var buf = &fixedbuf[_bufPtrM];
        var bitBuf = _bitBufM;
        var bitLeft = _bitLeftM;
        var crc16 = _crc16M;
        fixed (ushort* crc16T = Crc16.Table)
        {
            for (var i = count; i > 0; i--)
            {
                var vi = *residual++;
                var v = (uint)((vi << 1) ^ (vi >> 31));

                // write quotient in unary
                var q = (int)(v >> k) + 1;
                var bits = k + q;
                while (bits > 64)
                {
#if DEBUG
                    if (buf + 1 > fixedbuf + _bufEnd)
                    {
                        _eof = true;
                        return;
                    }
#endif
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bitBuf >> 56))]);
                    bitBuf <<= 8;
                    bits -= 8;
                }

                // write remainder in binary using 'k' bits
                //writebits_fast(k + q, (uint)((v & ((1 << k) - 1)) | (1 << k)), ref buf);
                ulong val = (v & ((1U << k) - 1)) | (1U << k);
                if (bits < bitLeft)
                {
                    bitLeft -= bits;
                    bitBuf |= val << bitLeft;
                }
                else
                {
                    var bb = bitBuf | (val >> (bits - bitLeft));
#if DEBUG
                    if (buf + 8 > fixedbuf + _bufEnd)
                    {
                        _eof = true;
                        return;
                    }
#endif

                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 56))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 48))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 40))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 32))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 24))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 16))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)(bb >> 8))]);
                    crc16 = (ushort)((crc16 << 8) ^ crc16T[(crc16 >> 8) ^ (*buf++ = (byte)bb)]);

                    bitLeft += 64 - bits;
                    bitBuf = val << (bitLeft - 1) << 1;
                }
            }
        }

        _crc16M = crc16;
        _bufPtrM = (int)(buf - fixedbuf);
        _bitBufM = bitBuf;
        _bitLeftM = bitLeft;
    }

    public void Flush()
    {
        while (_bitLeftM < 64 && !_eof)
        {
            if (_bufPtrM >= _bufEnd)
            {
                _eof = true;
                break;
            }
            if (Buffer != null)
            {
                var b = (byte)(_bitBufM >> 56);
                _crc16M = (ushort)((_crc16M << 8) ^ Crc16.Table[(_crc16M >> 8) ^ b]);
                Buffer[_bufPtrM] = b;
            }
            _bufPtrM++;
            _bitBufM <<= 8;
            _bitLeftM += 8;
        }
        _bitLeftM = 64;
        _bitBufM = 0;
    }
}