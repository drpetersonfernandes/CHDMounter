using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class Iso9660Parser
{
    private readonly SectorReader _reader;
    private bool _isHighSierra;
    private bool _isJoliet;
    private int _lbaOffset;

    public Iso9660Parser(SectorReader reader)
    {
        _reader = reader;
    }

    public void SetLbaOffset(int offset)
    {
        _lbaOffset = offset;
    }

    public bool Parse(FsNode rootNode, TrackInfo? track = null)
    {
        _reader.Reset();
        _reader.SetTrack(track, true);
        _reader.LbaOffset = _lbaOffset;
        _isHighSierra = false;
        _isJoliet = false;

        var trackStartLba = track?.StartLBA ?? 0;
        var effectiveTrackStart = _lbaOffset < 0 ? 45000u : trackStartLba;

        uint[] vdOffsets = [16, 17, 166, 167]; // 16+150, 17+150
        uint pvdLba = 0;
        var foundPvd = false;
        byte[] bestVdData = null;
        var sectorData = new byte[2048];

        foreach (var offset in vdOffsets)
        {
            if (_reader.ReadSector(effectiveTrackStart + offset, sectorData) && sectorData.Length >= 16)
            {
                var type = sectorData[0];
                var isIso = CheckMagic(sectorData, 1, "CD001");
                var isHs = CheckMagic(sectorData, 9, "CDROM");

                if (isIso || isHs)
                {
                    if (type == 2 && isIso) { pvdLba = effectiveTrackStart + offset;
                        _isHighSierra = false;
                        _isJoliet = true;
                        foundPvd = true;
                        bestVdData = sectorData;
                        break; }
                    if (!foundPvd && (type == 1 || isHs)) { pvdLba = effectiveTrackStart + offset;
                        _isHighSierra = isHs;
                        _isJoliet = false;
                        foundPvd = true;
                        bestVdData = sectorData; }
                }
            }
        }

        if (!foundPvd && effectiveTrackStart != 0)
        {
            foreach (var offset in vdOffsets)
            {
                if (_reader.ReadSector(offset, sectorData) && sectorData.Length >= 16)
                {
                    var type = sectorData[0];
                    var isIso = CheckMagic(sectorData, 1, "CD001");
                    var isHs = CheckMagic(sectorData, 9, "CDROM");
                    if (isIso || isHs)
                    {
                        if (type == 2 && isIso) { pvdLba = offset;
                            effectiveTrackStart = 0;
                            _reader.SetTrack(null);
                            _isHighSierra = false;
                            _isJoliet = true;
                            foundPvd = true;
                            bestVdData = sectorData;
                            break; }
                        if (!foundPvd && (type == 1 || isHs)) { pvdLba = offset;
                            effectiveTrackStart = 0;
                            _reader.SetTrack(null);
                            _isHighSierra = isHs;
                            _isJoliet = false;
                            foundPvd = true;
                            bestVdData = sectorData; }
                    }
                }
            }
        }

        if (!foundPvd)
        {
            for (uint i = 0; i < 100; i++)
            {
                if (_reader.ReadSector(effectiveTrackStart + i, sectorData) && sectorData.Length >= 16)
                {
                    var type = sectorData[0];
                    if (type is 1 or 2 && (CheckMagic(sectorData, 1, "CD001") || CheckMagic(sectorData, 9, "CDROM")))
                    { pvdLba = effectiveTrackStart + i;
                        _isHighSierra = CheckMagic(sectorData, 9, "CDROM");
                        _isJoliet = type == 2;
                        foundPvd = true;
                        bestVdData = sectorData;
                        break; }
                }
            }
        }

        if (!foundPvd)
            return false;

        var rootOff = _isHighSierra ? 180 : 156;
        var rootRelLba = LeU32(bestVdData!, rootOff + 2);
        var rootSize = LeU32(bestVdData!, rootOff + 10);

        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = effectiveTrackStart + rootRelLba + (uint)(_lbaOffset < 0 ? _lbaOffset : 0);
        rootNode.Size = rootSize;
        rootNode.Extents.Add(new FsExtent { Lba = rootNode.Lba, Size = rootSize });

        return ParseDirectory(rootNode, effectiveTrackStart);
    }

    public bool ParseDirectory(FsNode dirNode, uint trackStart)
    {
        var sectorsToRead = (uint)((dirNode.Size + 2047) / 2048);
        var sectorData = new byte[2048];

        for (uint i = 0; i < sectorsToRead; i++)
        {
            var currentLba = dirNode.Lba + i;
            if (!_reader.ReadSector(currentLba, sectorData))
                break;

            uint pos = 0;
            while (pos < 2048)
            {
                var recordLen = sectorData[pos];
                if (recordLen == 0) break;

                if (pos + recordLen > 2048 || recordLen < 34) { pos += recordLen;
                    if ((pos & 1) != 0)
                    {
                        pos++;
                    }

                    continue; }

                var relLba = LeU32(sectorData, (int)(pos + 2));
                ulong extentSize = LeU32(sectorData, (int)(pos + 10));

                var flagsOff = _isHighSierra ? 24 : 25;
                var flags = sectorData[pos + flagsOff];
                var isDir = (flags & 0x02) != 0;
                var isMulti = (flags & 0x80) != 0;

                var nameLenOff = _isHighSierra ? 31 : 32;
                var nameLen = sectorData[pos + nameLenOff];
                var nameOff = _isHighSierra ? 32 : 33;

                if (nameOff + nameLen > recordLen || pos + nameOff + nameLen > 2048)
                { pos += recordLen;
                    if ((pos & 1) != 0)
                    {
                        pos++;
                    }

                    continue; }

                var name = DecodeName(sectorData, (int)pos + nameOff, nameLen);

                if (name != "." && name != "..")
                {
                    var absoluteLba = trackStart + relLba;

                    if (dirNode.Children.Count > 0 && dirNode.Children[^1].IsMultiExtent && !dirNode.Children[^1].IsDirectory
                        && dirNode.Children[^1].Name == name)
                    {
                        dirNode.Children[^1].Size += extentSize;
                        dirNode.Children[^1].Extents.Add(new FsExtent { Lba = absoluteLba, Size = extentSize });
                        dirNode.Children[^1].IsMultiExtent = isMulti;
                    }
                    else
                    {
                        var child = new FsNode { Name = name, Lba = absoluteLba, Size = extentSize, IsDirectory = isDir, IsMultiExtent = isMulti };
                        child.Extents.Add(new FsExtent { Lba = child.Lba, Size = child.Size });
                        if (child.IsDirectory) ParseDirectory(child, trackStart);
                        dirNode.Children.Add(child);
                    }
                }

                pos += recordLen;
                if ((pos & 1) != 0)
                {
                    pos++;
                }
            }
        }
        return true;
    }

    private string DecodeName(byte[] data, int offset, byte nameLen)
    {
        if (_isJoliet) return DecodeUtf16Be(data, offset, nameLen);

        switch (nameLen)
        {
            case 1 when data[offset] == 0x00:
                return ".";
            case 1 when data[offset] == 0x01:
                return "..";
        }

        var name = Encoding.ASCII.GetString(data, offset, nameLen);
        var semi = name.IndexOf(';');
        if (semi >= 0)
        {
            name = name[..semi];
        }

        if (name.EndsWith('.'))
        {
            name = name[..^1];
        }

        return name;
    }

    private static string DecodeUtf16Be(byte[] data, int offset, int len)
    {
        switch (len)
        {
            case 1 when data[offset] == 0x00:
                return ".";
            case 1 when data[offset] == 0x01:
                return "..";
        }

        var sb = new StringBuilder();
        for (var i = 0; i + 1 < len; i += 2)
        {
            var u16 = (ushort)((data[offset + i] << 8) | data[offset + i + 1]);
            if (u16 == 0) break;

            sb.Append(Utf16ToChar(u16));
        }
        var name = sb.ToString();
        var semi = name.IndexOf(';');
        return semi >= 0 ? name[..semi] : name;
    }

    private static string Utf16ToChar(ushort u16)
    {
        return u16 switch
        {
            < 0x80 => ((char)u16).ToString(),
            < 0x800 => $"{(char)(0xC0 | (u16 >> 6))}{(char)(0x80 | (u16 & 0x3F))}",
            _ => $"{(char)(0xE0 | (u16 >> 12))}{(char)(0x80 | ((u16 >> 6) & 0x3F))}{(char)(0x80 | (u16 & 0x3F))}"
        };
    }

    private static bool CheckMagic(byte[] data, int offset, string magic)
    {
        return Encoding.ASCII.GetString(data, offset, magic.Length) == magic;
    }

    private static uint LeU32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}
