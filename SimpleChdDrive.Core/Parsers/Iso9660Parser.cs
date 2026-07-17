using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class Iso9660Parser
{
    private const int MaxDirectoryDepth = 64;
    private const int MaxCeChain = 64;

    private readonly SectorReader _reader;
    private bool _isHighSierra;
    private bool _isJoliet;
    private bool _isXa;
    private bool _suspActive;
    private byte _suspSkip;
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
        if (track is { Frames: > 0 })
            _reader.SetTrack(track, true);
        else
            _reader.SetTrack(null!);
        _reader.LbaOffset = _lbaOffset;
        _isHighSierra = false;
        _isJoliet = false;

        var trackStartLba = track?.StartLba ?? 0;
        var effectiveTrackStart = _lbaOffset < 0 ? 45000u : trackStartLba;

        uint[] vdOffsets = [16, 17, 166, 167]; // 16+150, 17+150
        var foundPvd = false;
        byte[]? bestVdData = null;
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
                    if (type == 2 && isIso) {
                        _isHighSierra = false;
                        _isJoliet = true;
                        foundPvd = true;
                        bestVdData = sectorData;
                        break; }

                    if (!foundPvd && (type == 1 || isHs)) {
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
                        if (type == 2 && isIso) {
                            effectiveTrackStart = 0;
                            _reader.SetTrack(null!);
                            _isHighSierra = false;
                            _isJoliet = true;
                            foundPvd = true;
                            bestVdData = sectorData;
                            break; }

                        if (!foundPvd && (type == 1 || isHs)) {
                            effectiveTrackStart = 0;
                            _reader.SetTrack(null!);
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
                    {
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
        string? illMultiExtentName = null;

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

                if (pos + recordLen > 2048 || recordLen < 34)
                    break;

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
                    var skipRecord = false;

                    if (illMultiExtentName != null)
                    {
                        if (name == illMultiExtentName && !isDir)
                        {
                            if (!isMulti)
                            {
                                illMultiExtentName = null;
                            }

                            skipRecord = true;
                        }
                        else
                        {
                            illMultiExtentName = null;
                        }
                    }

                    if (!skipRecord)
                    {
                        var last = dirNode.Children.Count > 0 ? dirNode.Children[^1] : null;
                        if (last is { IsMultiExtent: true, IsDirectory: false } && last.Name == name && !isDir)
                        {
                            if (last.Extents.Count > 0 && last.Extents[^1].Size % 2048 != 0)
                            {
                                last.IsMultiExtent = false;
                                if (isMulti)
                                {
                                    illMultiExtentName = name;
                                }
                            }
                            else
                            {
                                last.Size += extentSize;
                                last.Extents.Add(new FsExtent { Lba = absoluteLba, Size = extentSize });
                                last.IsMultiExtent = isMulti;
                            }
                        }
                        else
                        {
                            var child = new FsNode { Name = name, Lba = absoluteLba, Size = extentSize, IsDirectory = isDir, IsMultiExtent = isMulti };
                            child.Extents.Add(new FsExtent { Lba = child.Lba, Size = child.Size });
                            if (child.IsDirectory) ParseDirectory(child, trackStart);
                            dirNode.Children.Add(child);
                        }
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

        var name = Encoding.BigEndianUnicode.GetString(data, offset, len & ~1);
        var nul = name.IndexOf('\0');
        if (nul >= 0)
        {
            name = name[..nul];
        }

        var semi = name.IndexOf(';');
        return semi >= 0 ? name[..semi] : name;
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
