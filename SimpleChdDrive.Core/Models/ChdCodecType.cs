namespace SimpleChdDrive.Core.Models;

public enum ChdCodecType
{
    None = 0,
    Zlib = 0x7A6C6962,
    Lzma = 0x6C7A6D61,
    Huffman = 0x68756666,
    Flac = 0x666C6163,
    Zstd = 0x7A737464,
    Cdzl = 0x63647A6C,
    Cdlz = 0x63646C7A,
    Cdfl = 0x6364666C,
    Cdzs = 0x63647A73,
    Avhu = 0x61766875,
    Error = 0x0eeeeeee
}
