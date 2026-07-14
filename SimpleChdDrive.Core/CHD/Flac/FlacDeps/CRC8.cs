namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class Crc8
{
    private const ushort Poly8 = 0x07;

    private static ushort[] _table;

    public Crc8()
    {
        if (_table != null)
            return;

        _table = new ushort[256];
        const int bits = 8;
        const ushort poly = (ushort)(Poly8 + (1U << bits));
        for (ushort i = 0; i < _table.Length; i++)
        {
            var crc = i;
            for (var j = 0; j < bits; j++)
            {
                if ((crc & (1U << (bits - 1))) != 0)
                {
                    crc = (ushort)((crc << 1) ^ poly);
                }
                else
                {
                    crc <<= 1;
                }
            }
            _table[i] = (ushort)(crc & 0x00ff);
        }
    }

    public byte ComputeChecksum(byte[] bytes, int pos, int count)
    {
        ushort crc = 0;
        for (var i = pos; i < pos + count; i++)
        {
            crc = _table[crc ^ bytes[i]];
        }

        return (byte)crc;
    }

    public unsafe byte ComputeChecksum(byte* bytes, int pos, int count)
    {
        ushort crc = 0;
        for (var i = pos; i < pos + count; i++)
        {
            crc = _table[crc ^ bytes[i]];
        }

        return (byte)crc;
    }
}