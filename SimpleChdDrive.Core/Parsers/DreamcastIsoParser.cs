using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class DreamcastIsoParser
{
    private readonly SectorReader _reader;
    private bool _isHighSierra;
    private bool _isJoliet;
    private int _lbaOffset;

    public DreamcastIsoParser(SectorReader reader) { _reader = reader; }
    public void SetLbaOffset(int offset) => _lbaOffset = offset;

    public bool Parse(FsNode rootNode, TrackInfo track = null)
    {
        _reader.Reset();
        _reader.SetTrack(track, true);
        _reader.LbaOffset = _lbaOffset;
        _isHighSierra = false;
        _isJoliet = false;

        uint trackStartLba = track?.StartLBA ?? 0;
        byte[] sectorData = new byte[2048];

        var volumeStarts = new List<uint>();
        if (_lbaOffset >= 45000) { volumeStarts.Add(0); volumeStarts.Add(150); }
        else { volumeStarts.Add(trackStartLba); if (trackStartLba >= 45000) volumeStarts.Add(trackStartLba + 150); }

        uint effectiveStart = volumeStarts[0];
        bool foundPvd = false;
        byte[] bestVdData = null;

        foreach (uint startLba in volumeStarts)
        {
            foreach (uint offset in new uint[] { 16, 17 })
            {
                uint readLba = startLba + offset;
                if (_reader.ReadSector(readLba, sectorData) && sectorData.Length >= 16)
                {
                    byte type = sectorData[0];
                    bool isIso = CheckMagic(sectorData, 1, "CD001");
                    bool isHs = CheckMagic(sectorData, 9, "CDROM");

                    if (isIso || isHs)
                    {
                        if (type == 2 && isIso) { effectiveStart = startLba; _isHighSierra = false; _isJoliet = true; foundPvd = true; bestVdData = sectorData; break; }
                        if (!foundPvd && type == 1) { effectiveStart = startLba; _isHighSierra = isHs; _isJoliet = false; foundPvd = true; bestVdData = sectorData; }
                    }
                }
            }
            if (_isJoliet) break;
            if (foundPvd) break;
        }

        if (!foundPvd)
        {
            foreach (uint startLba in volumeStarts)
            {
                for (uint i = 0; i < 300; i++)
                {
                    uint readLba = startLba + i;
                    if (_reader.ReadSector(readLba, sectorData) && sectorData.Length >= 16 && (sectorData[0] == 1 || sectorData[0] == 2))
                    {
                        if (CheckMagic(sectorData, 1, "CD001") || CheckMagic(sectorData, 9, "CDROM"))
                        {
                            effectiveStart = readLba - (uint)(sectorData[0] == 1 ? 16 : 17);
                            _isHighSierra = CheckMagic(sectorData, 9, "CDROM");
                            _isJoliet = sectorData[0] == 2;
                            foundPvd = true;
                            bestVdData = sectorData;
                            break;
                        }
                    }
                }
                if (foundPvd) break;
            }
        }

        if (!foundPvd) return false;

        int rootOff = _isHighSierra ? 180 : 156;
        uint rootExtentLba = LeU32(bestVdData!, rootOff + 2);
        uint rootSize = LeU32(bestVdData!, rootOff + 10);

        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = effectiveStart + rootExtentLba;
        rootNode.Size = rootSize;
        rootNode.Extents.Add(new FsExtent { Lba = rootNode.Lba, Size = rootSize });

        return ParseDirectory(rootNode, effectiveStart);
    }

    private bool ParseDirectory(FsNode dirNode, uint volumeStart)
    {
        uint sectorsToRead = (uint)((dirNode.Size + 2047) / 2048);
        if (sectorsToRead == 0 && dirNode.Size > 0) sectorsToRead = 1;
        byte[] sectorData = new byte[2048];

        for (uint i = 0; i < sectorsToRead; i++)
        {
            uint currentLba = dirNode.Lba + i;
            if (!_reader.ReadSector(currentLba, sectorData)) break;

            uint pos = 0;
            while (pos < 2048)
            {
                byte recordLen = sectorData[pos];
                if (recordLen == 0) break;
                if (pos + recordLen > 2048 || recordLen < 34) { pos += recordLen; if ((pos & 1) != 0) pos++; continue; }

                uint extentLba = LeU32(sectorData, (int)pos + 2);
                ulong extentSize = LeU32(sectorData, (int)pos + 10);

                int flagsOff = _isHighSierra ? 24 : 25;
                byte flags = sectorData[pos + flagsOff];
                bool isDir = (flags & 0x02) != 0;
                bool isMulti = (flags & 0x80) != 0;

                int nameLenOff = _isHighSierra ? 31 : 32;
                byte nameLen = sectorData[pos + nameLenOff];
                int nameOff = _isHighSierra ? 32 : 33;

                if (nameOff + nameLen > recordLen || (int)pos + nameOff + nameLen > 2048)
                { pos += recordLen; if ((pos & 1) != 0) pos++; continue; }

                string name = DecodeName(sectorData, (int)pos + nameOff, nameLen);

                if (name != "." && name != "..")
                {
                    uint absoluteLba = extentLba >= volumeStart ? extentLba : volumeStart + extentLba;

                    if (dirNode.Children.Count > 0 && isMulti && !dirNode.Children[^1].IsDirectory && dirNode.Children[^1].Name == name)
                    {
                        dirNode.Children[^1].Size += extentSize;
                        dirNode.Children[^1].Extents.Add(new FsExtent { Lba = absoluteLba, Size = extentSize });
                    }
                    else
                    {
                        var child = new FsNode { Name = name, Lba = absoluteLba, Size = extentSize, IsDirectory = isDir };
                        child.Extents.Add(new FsExtent { Lba = child.Lba, Size = child.Size });
                        if (child.IsDirectory) ParseDirectory(child, volumeStart);
                        dirNode.Children.Add(child);
                    }
                }

                pos += recordLen;
                if ((pos & 1) != 0) pos++;
            }
        }
        return true;
    }

    private string DecodeName(byte[] data, int offset, byte nameLen)
    {
        if (_isJoliet) return DecodeUtf16Be(data, offset, nameLen);
        if (nameLen == 1 && data[offset] == 0x00) return ".";
        if (nameLen == 1 && data[offset] == 0x01) return "..";
        var name = Encoding.ASCII.GetString(data, offset, nameLen);
        int semi = name.IndexOf(';');
        if (semi >= 0) name = name[..semi];
        if (name.EndsWith('.')) name = name[..^1];
        return name;
    }

    private static string DecodeUtf16Be(byte[] data, int offset, int len)
    {
        if (len == 1 && data[offset] == 0x00) return ".";
        if (len == 1 && data[offset] == 0x01) return "..";
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < len; i += 2)
        {
            ushort u16 = (ushort)((data[offset + i] << 8) | data[offset + i + 1]);
            if (u16 == 0) break;
            sb.Append(char.ConvertFromUtf32(u16));
        }
        var name = sb.ToString();
        int semi = name.IndexOf(';');
        return semi >= 0 ? name[..semi] : name;
    }

    private static bool CheckMagic(byte[] d, int o, string m)
    { for (int i = 0; i < m.Length; i++) if (d[o + i] != m[i]) return false; return true; }
    private static uint LeU32(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
}
