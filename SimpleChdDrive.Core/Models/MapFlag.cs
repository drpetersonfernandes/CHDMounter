namespace SimpleChdDrive.Core.Models;

[Flags]
public enum MapFlag
{
    TypeMask = 0x000f,
    NoCrc = 0x0010,

    Invalid = 0x0000,
    Compressed = 0x0001,
    Uncompressed = 0x0002,
    Mini = 0x0003,
    SelfHunk = 0x0004,
    ParentHunk = 0x0005
}
