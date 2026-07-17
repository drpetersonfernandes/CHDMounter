using System.Buffers.Binary;
using System.Text;

namespace SimpleChdDrive.Core.Parsers;

internal static class ChdHeaderReader
{
    public static uint ReadUnitBytes(string chdPath)
    {
        using var fs = new FileStream(chdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> buf = stackalloc byte[48];

        if (fs.Read(buf[..16]) < 16)
            return 0;

        if (Encoding.ASCII.GetString(buf[..8]) != "MComprHD")
            return 0;

        var version = BinaryPrimitives.ReadUInt32BigEndian(buf[12..]);

        return version switch
        {
            1 or 2 => ReadV1V2UnitBytes(fs),
            3 => ReadV3UnitBytes(fs),
            4 => ReadV4UnitBytes(fs),
            5 => ReadV5UnitBytes(fs),
            _ => 0
        };
    }

    private static uint ReadV1V2UnitBytes(FileStream fs)
    {
        Span<byte> buf = stackalloc byte[8];
        fs.Seek(24, SeekOrigin.Begin);
        fs.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt32BigEndian(buf);
    }

    private static uint ReadV3UnitBytes(FileStream fs)
    {
        Span<byte> buf = stackalloc byte[4];
        fs.Seek(44, SeekOrigin.Begin);
        fs.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt32BigEndian(buf);
    }

    private static uint ReadV4UnitBytes(FileStream fs)
    {
        Span<byte> buf = stackalloc byte[4];
        fs.Seek(40, SeekOrigin.Begin);
        fs.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt32BigEndian(buf);
    }

    private static uint ReadV5UnitBytes(FileStream fs)
    {
        Span<byte> buf = stackalloc byte[4];
        fs.Seek(44, SeekOrigin.Begin);
        fs.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt32BigEndian(buf);
    }
}
