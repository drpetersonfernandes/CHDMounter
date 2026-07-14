using System.Text;
using SimpleChdDrive.Core.CHD.Utils;

namespace SimpleChdDrive.Core.CHD;

internal class ChdHeader
{
    public ChdCodecType[] Compression = null!;
    public ChdReader[] ChdReader = null!;

    public ulong Totalbytes;
    public uint Blocksize;
    public uint Totalblocks;

    public uint Unitbytes;

    public bool UncompressedMap;

    public MapEntry[] Map = null!;

    public byte[] Md5 = null!;
    public byte[] Rawsha1 = null!;
    public byte[] Sha1 = null!;

    public byte[] Parentmd5 = null!;
    public byte[] Parentsha1 = null!;

    public ulong Metaoffset;
}

internal class MapEntry
{
    public CompressionType Comptype;
    public uint Length;
    public ulong Offset;
    public uint? Crc;
    public ushort? Crc16;

    public MapEntry SelfMapEntry = null!;

    public int UseCount;

    public byte[] BuffIn = null!;
    public byte[] BuffOutCache = null!;
}

public static class Chd
{
    public const int TaskCount = 8;

    private static readonly uint[] HeaderLengths = [0, 76, 80, 120, 108, 124];
    private static readonly byte[] Id = "MComprHD"u8.ToArray();

    public static bool CheckHeader(Stream file, out uint length, out uint version)
    {
        foreach (var t in Id)
        {
            var b = (byte)file.ReadByte();
            if (b != t)
            {
                length = 0;
                version = 0;
                return false;
            }
        }

        using var br = new BinaryReader(file, Encoding.UTF8, true);
        length = br.ReadUInt32Be();
        version = br.ReadUInt32Be();
        return HeaderLengths[version] == length;
    }
}
