namespace SimpleChdDrive.Core.Parsers;

public class ThreeDoParser
{
    private readonly SectorReader _reader;

    private static readonly byte[] OperaMagic = [0x01, 0x5A, 0x5A, 0x5A, 0x5A, 0x5A, 0x01];

    public ThreeDoParser(SectorReader reader)
    {
        _reader = reader;
    }

    public bool Parse(FsNode rootNode, TrackInfo track = null)
    {
        _reader.Reset();
        _reader.SetTrack(track, true);

        byte[] sectorData = new byte[2048];
        uint trackStart = track?.StartLBA ?? 0;
        bool foundVh = false;

        if (_reader.ReadSector(trackStart, sectorData) && CheckMagic(sectorData, 0, OperaMagic))
            foundVh = true;

        if (!foundVh)
        {
            for (uint i = 0; i < 100; i++)
            {
                if (_reader.ReadSector(trackStart + i, sectorData) && CheckMagic(sectorData, 0, OperaMagic))
                { trackStart += i; foundVh = true; break; }
            }
        }

        if (!foundVh) return false;

        uint blockSize = Be24(sectorData, 0x4D);
        if (blockSize == 0) blockSize = 2048;

        uint avatarsCount = sectorData[0x0F];
        if (avatarsCount > 8) avatarsCount = 7;

        uint rootDirBlock = Be24(sectorData, 0x11);

        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = trackStart + rootDirBlock * (blockSize / 2048);
        rootNode.Size = 0;

        return ParseDirectory(rootDirBlock, blockSize, rootNode, trackStart);
    }

    private bool ParseDirectory(uint dirBlock, uint blockSize, FsNode parentNode, uint trackStart)
    {
        byte[] sectorData = new byte[2048];
        uint currentBlock = dirBlock;
        uint baseBlockLocation = dirBlock;
        var visited = new HashSet<uint>();

        while (true)
        {
            if (visited.Contains(currentBlock)) break;
            visited.Add(currentBlock);

            uint currentLba = trackStart + currentBlock * (blockSize / 2048);
            if (!_reader.ReadSector(currentLba, sectorData)) return false;

            uint firstEntryOffset = Be16(sectorData, 0x12);
            if (firstEntryOffset == 0 || firstEntryOffset >= 2048) firstEntryOffset = 0x14;

            uint nextBlockOffset = Be16(sectorData, 0x02);

            uint pos = firstEntryOffset;
            while (pos + 72 <= 2048)
            {
                uint flags = Be32(sectorData, (int)pos);
                bool isLast = (flags & 0x80000000) != 0;

                if (flags == 0 && sectorData[pos + 0x20] == 0) break;

                bool isDir = (flags & 0x07) == 0x07;

                string name = System.Text.Encoding.ASCII.GetString(sectorData, (int)pos + 0x20, 32).TrimEnd('\0');

                uint byteCount = Be24(sectorData, (int)pos + 0x11);
                uint avCnt = sectorData[pos + 0x43];
                if (avCnt > 255) break;

                uint extent = Be24(sectorData, (int)pos + 0x45);

                var child = new FsNode
                {
                    Name = name,
                    Lba = trackStart + extent * (blockSize / 2048),
                    Size = byteCount,
                    IsDirectory = isDir
                };

                if (child.IsDirectory && extent != 0 && extent != currentBlock)
                    ParseDirectory(extent, blockSize, child, trackStart);

                parentNode.Children.Add(child);

                pos += 0x48 + avCnt * 4;
                if (isLast) break;
            }

            if (nextBlockOffset == 0xFFFF) break;
            currentBlock = baseBlockLocation + nextBlockOffset;
        }
        return true;
    }

    private static bool CheckMagic(byte[] d, int o, byte[] m)
    { for (int i = 0; i < m.Length; i++) { if (d[o + i] != m[i]) return false; } return true; }

    private static uint Be24(byte[] d, int o) => (uint)((d[o] << 16) | (d[o + 1] << 8) | d[o + 2]);
    private static uint Be16(byte[] d, int o) => (uint)((d[o] << 8) | d[o + 1]);
    private static uint Be32(byte[] d, int o) => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
}
