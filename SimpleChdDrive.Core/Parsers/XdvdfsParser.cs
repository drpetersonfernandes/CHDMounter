using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class XdvdfsParser
{
    private readonly SectorReader _reader;
    private TrackInfo _currentTrack;

    public XdvdfsParser(SectorReader reader)
    {
        _reader = reader;
    }

    public void SetTrack(TrackInfo track)
    {
        _currentTrack = track;
        _reader.SetTrack(track, true);
    }

    private static readonly byte[] XdvdfsMagic = "MICROSOFT*XBOX*MEDIA"u8.ToArray();

    public bool Parse(FsNode rootNode)
    {
        _reader.Reset();
        if (_currentTrack != null) _reader.SetTrack(_currentTrack, true);

        var sectorData = new byte[2048];
        uint volumeOffsetSectors = 0;
        uint rootDirSector = 0;
        uint rootDirExtentSize = 0;
        var found = false;

        uint[] offsets = [32, 129856, 16672, 198176, 0];

        foreach (var offset in offsets)
        {
            if (_reader.ReadSector(offset, sectorData))
            {
                if (CheckMagic(sectorData, 0, XdvdfsMagic) && CheckMagic(sectorData, 0x7EC, XdvdfsMagic))
                {
                    rootDirSector = LeU32(sectorData, 20);
                    rootDirExtentSize = LeU32(sectorData, 24);

                    volumeOffsetSectors = offset switch
                    {
                        32 or 0 => 0,
                        129856 => 129824,
                        16672 => 16640,
                        198176 => 198144,
                        _ => 0
                    };
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            for (uint offset = 0; offset < 102400; offset++)
            {
                if (offsets.Contains(offset)) continue;

                if (_reader.ReadSector(offset, sectorData))
                {
                    if (CheckMagic(sectorData, 0, XdvdfsMagic) && CheckMagic(sectorData, 0x7EC, XdvdfsMagic))
                    {
                        rootDirSector = LeU32(sectorData, 20);
                        rootDirExtentSize = LeU32(sectorData, 24);
                        volumeOffsetSectors = offset >= 32 ? offset - 32 : 0;
                        found = true;
                        break;
                    }
                }
            }
        }

        if (!found) return false;

        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = volumeOffsetSectors + rootDirSector;
        rootNode.Size = 0;

        var visited = new HashSet<ulong>();
        return ParseDirectoryTree(rootNode.Lba, 0, rootNode, volumeOffsetSectors, rootDirExtentSize, 0, visited, true);
    }

    private bool ParseDirectoryTree(uint dirSector, uint dirOffset, FsNode parentNode,
        uint volumeOffsetSectors, uint dirExtentSize, int depth, HashSet<ulong> visited, bool inLlCompat)
    {
        if (depth > 2048) return false;

        if (dirOffset >= dirExtentSize) return true;

        var absoluteSector = dirSector + dirOffset / 2048;
        var offsetInSector = dirOffset % 2048;

        var nodeId = ((ulong)absoluteSector << 32) | offsetInSector;
        if (visited.Contains(nodeId)) return true;

        visited.Add(nodeId);

        var sectorData = new byte[2048];
        if (!_reader.ReadSector(absoluteSector, sectorData)) return false;

        if (offsetInSector + 14 > 2048)
        {
            var nextOffset = dirOffset + (2048 - offsetInSector);
            return ParseDirectoryTree(dirSector, nextOffset, parentNode, volumeOffsetSectors, dirExtentSize, depth, visited, inLlCompat);
        }

        var entryOff = offsetInSector;
        var leftSubTree = LeU16(sectorData, (int)entryOff);
        var rightSubTree = LeU16(sectorData, (int)(entryOff + 2));
        var startSector = LeU32(sectorData, (int)(entryOff + 4));
        var fileSize = LeU32(sectorData, (int)(entryOff + 8));
        var attributes = sectorData[entryOff + 12];
        var nameLen = sectorData[entryOff + 13];

        if (leftSubTree == 0xFFFF)
        {
            if (dirOffset == 0) return true;

            var nextOffset = dirOffset + (2048 - dirOffset % 2048);
            if (nextOffset >= dirExtentSize) return true;

            return ParseDirectoryTree(dirSector, nextOffset, parentNode, volumeOffsetSectors, dirExtentSize, depth, visited, inLlCompat);
        }

        if (offsetInSector + 14 + nameLen > 2048)
        {
            var nextOffset = dirOffset + (2048 - offsetInSector);
            if (nextOffset >= dirExtentSize) return true;

            return ParseDirectoryTree(dirSector, nextOffset, parentNode, volumeOffsetSectors, dirExtentSize, depth, visited, inLlCompat);
        }

        var localLlCompat = inLlCompat;
        if (leftSubTree != 0 && leftSubTree != 0xFFFF)
        {
            localLlCompat = false;
            ParseDirectoryTree(dirSector, (uint)(leftSubTree * 4), parentNode, volumeOffsetSectors, dirExtentSize, depth + 1, visited, localLlCompat);
        }

        if (nameLen > 0)
        {
            var node = new FsNode
            {
                Name = Encoding.ASCII.GetString(sectorData, (int)(entryOff + 14), nameLen),
                Lba = volumeOffsetSectors + startSector,
                Size = fileSize,
                IsDirectory = (attributes & 0x10) != 0
            };

            if (node.IsDirectory && node.Size > 0)
            {
                var subVisited = new HashSet<ulong>();
                ParseDirectoryTree(node.Lba, 0, node, volumeOffsetSectors, fileSize, depth + 1, subVisited, localLlCompat);
            }
            parentNode.Children.Add(node);
        }

        if (rightSubTree != 0 && rightSubTree != 0xFFFF)
        {
            ParseDirectoryTree(dirSector, (uint)(rightSubTree * 4), parentNode, volumeOffsetSectors, dirExtentSize, depth + 1, visited, localLlCompat);
        }

        return true;
    }

    private static bool CheckMagic(byte[] data, int offset, byte[] magic)
    {
        for (var i = 0; i < magic.Length; i++)
            if (data[offset + i] != magic[i]) return false;

        return true;
    }

    private static ushort LeU16(byte[] d, int o)
    {
        return (ushort)(d[o] | (d[o + 1] << 8));
    }

    private static uint LeU32(byte[] d, int o)
    {
        return (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
    }
}
