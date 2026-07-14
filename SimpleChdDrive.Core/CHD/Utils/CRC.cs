namespace SimpleChdDrive.Core.CHD.Utils;

public class Crc
{
    public static readonly uint[] Crc32Lookup;
    private uint _crc;

    static Crc()
    {
        const uint polynomial = 0xEDB88320;
        const int crcNumTables = 8;

        unchecked
        {
            Crc32Lookup = new uint[256 * crcNumTables];
            int i;
            for (i = 0; i < 256; i++)
            {
                var r = (uint)i;
                for (var j = 0; j < 8; j++)
                {
                    r = (r >> 1) ^ (polynomial & ~((r & 1) - 1));
                }

                Crc32Lookup[i] = r;
            }

            for (; i < 256 * crcNumTables; i++)
            {
                var r = Crc32Lookup[i - 256];
                Crc32Lookup[i] = Crc32Lookup[r & 0xFF] ^ (r >> 8);
            }
        }
    }


    public Crc()
    {
        Reset();
    }

    public void Reset()
    {
        TotalBytesRead = 0;
        _crc = 0xffffffffu;
    }


    internal void UpdateCrc(int inCh)
    {
        _crc = (_crc >> 8) ^ Crc32Lookup[(byte)_crc ^ (byte)inCh];
    }

    public void SlurpBlock(byte[] block, int offset, int count)
    {
        TotalBytesRead += count;
        var crc = _crc;

        for (; (offset & 7) != 0 && count != 0; count--)
        {
            crc = (crc >> 8) ^ Crc32Lookup[(byte)crc ^ block[offset++]];
        }

        if (count >= 8)
        {
            var end = (count - 8) & ~7;
            count -= end;
            end += offset;

            while (offset != end)
            {
                crc ^= (uint)(block[offset] + (block[offset + 1] << 8) + (block[offset + 2] << 16) + (block[offset + 3] << 24));
                var high = (uint)(block[offset + 4] + (block[offset + 5] << 8) + (block[offset + 6] << 16) + (block[offset + 7] << 24));
                offset += 8;

                crc = Crc32Lookup[(byte)crc + 0x700]
                      ^ Crc32Lookup[(byte)(crc >>= 8) + 0x600]
                      ^ Crc32Lookup[(byte)(crc >>= 8) + 0x500]
                      ^ Crc32Lookup[ /*(byte)*/(crc >> 8) + 0x400]
                      ^ Crc32Lookup[(byte)high + 0x300]
                      ^ Crc32Lookup[(byte)(high >>= 8) + 0x200]
                      ^ Crc32Lookup[(byte)(high >>= 8) + 0x100]
                      ^ Crc32Lookup[ /*(byte)*/(high >> 8) + 0x000];
            }
        }

        while (count-- != 0)
        {
            crc = (crc >> 8) ^ Crc32Lookup[(byte)crc ^ block[offset++]];
        }

        _crc = crc;

    }

    public byte[] Crc32ResultB
    {
        get
        {
            var result = BitConverter.GetBytes(~_crc);
            Array.Reverse(result);
            return result;
        }
    }
    public int Crc32Result => unchecked((int)~_crc);

    public uint Crc32ResultU => ~_crc;

    public long TotalBytesRead { get; private set; }

    public static uint CalculateDigest(byte[] data, uint offset, uint size)
    {
        var crc = new Crc();
        // crc.Init();
        crc.SlurpBlock(data, (int)offset, (int)size);
        return crc.Crc32ResultU;
    }

    public static bool VerifyDigest(uint digest, byte[] data, uint offset, uint size)
    {
        return CalculateDigest(data, offset, size) == digest;
    }
}