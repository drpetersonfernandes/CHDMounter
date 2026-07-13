using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class CDiFsParser
{
    private readonly SectorReader _reader;
    private int _lbaOffset;

    public CDiFsParser(SectorReader reader)
    {
        _reader = reader;
    }

    public bool Parse(FsNode rootNode, TrackInfo track = null)
    {
        _reader.Reset();
        _reader.SetTrack(track, true);
        _lbaOffset = 0;

        byte[] sectorData = new byte[2048];
        uint trackStart = track?.StartLBA ?? 0;

        uint pvdLba = 0;
        bool foundVd = false;
        byte[] bestVdData = null;

        for (uint offset = 0; offset < 100; offset++)
        {
            uint currentLba = trackStart + offset;
            if (!_reader.ReadSector(currentLba, sectorData)) continue;

            byte type = sectorData[0];
            bool hasCdi = CheckSig(sectorData, 1, "CD-I ") || CheckSig(sectorData, 8, "CD-RTOS");
            bool hasIso = CheckSig(sectorData, 1, "CD001");

            if (hasCdi || hasIso)
            {
                uint rootRelLba = ReadCDiU32(sectorData, 158);
                uint rootSize = ReadCDiU32(sectorData, 166);

                if (rootSize > 0 || ReadCDiU32(sectorData, 148) > 0)
                { pvdLba = currentLba; bestVdData = sectorData; foundVd = true; break; }
            }
            if (type == 255) break;
        }

        if (!foundVd) return false;

        byte[] vd = bestVdData!;
        uint rootRelLba2 = ReadCDiU32(vd, 158);
        uint rootSize2 = ReadCDiU32(vd, 166);

        if (rootSize2 == 0)
        {
            uint pathTableLba = BeU32(vd, 148);
            if (pathTableLba > 0 && _reader.ReadSector(trackStart + pathTableLba, sectorData))
            {
                rootRelLba2 = BeU32(sectorData, 2);
                rootSize2 = 2048;
            }
        }

        if (rootRelLba2 == 0) return false;

        rootNode.Name = "/";
        rootNode.IsDirectory = true;
        rootNode.Lba = trackStart + rootRelLba2 + (uint)_lbaOffset;
        rootNode.Size = rootSize2;

        return ParseDirectory(rootNode, trackStart);
    }

    private bool ParseDirectory(FsNode dirNode, uint trackStart)
    {
        uint size = dirNode.Size == 0 ? 2048 : (uint)dirNode.Size;
        byte[] sectorData = new byte[2048];

        for (uint i = 0; i < 256; i++)
        {
            if (!_reader.ReadSector(dirNode.Lba + i, sectorData)) break;

            uint pos = 0;
            bool hasRecords = false;
            while (pos < 2048)
            {
                byte recordLen = sectorData[pos];
                if (recordLen == 0) break;
                if (pos + recordLen > 2048 || recordLen < 34) break;

                hasRecords = true;
                uint relLba = ReadCDiU32(sectorData, (int)pos + 2);
                ulong fileSize = ReadCDiU32(sectorData, (int)pos + 10);
                byte nameLen = sectorData[pos + 32];

                if (33 + nameLen > recordLen || pos + 33 + nameLen > 2048)
                { pos += recordLen; if ((pos & 1) != 0) pos++; continue; }

                int suOffset = 33 + nameLen + ((nameLen & 1) != 0 ? 1 : 0);
                bool isDir = false;
                byte fileNumber = 0;
                bool isInterleaved = false;

                if (suOffset + 10 <= recordLen)
                {
                    ushort fileAttr = BeU16(sectorData, (uint)(pos + suOffset + 4));
                    isDir = (fileAttr & 0x8000) != 0;
                    fileNumber = sectorData[pos + suOffset + 8];
                }

                if (!isDir && recordLen > 25) isDir = (sectorData[pos + 25] & 0x02) != 0;
                if (recordLen > 26 && sectorData[pos + 26] > 1) isInterleaved = true;

                string name;
                if (nameLen == 1 && sectorData[pos + 33] == 0x00) name = ".";
                else if (nameLen == 1 && sectorData[pos + 33] == 0x01) name = "..";
                else
                {
                    name = Encoding.ASCII.GetString(sectorData, (int)pos + 33, nameLen);
                    int semi = name.IndexOf(';');
                    if (semi >= 0) name = name[..semi];
                }

                if (name != "." && name != "..")
                {
                    var child = new FsNode
                    {
                        Name = name,
                        Lba = trackStart + relLba + (uint)_lbaOffset,
                        Size = fileSize,
                        IsDirectory = isDir,
                        FileNumber = fileNumber,
                        IsInterleaved = isInterleaved
                    };
                    if (child.IsDirectory) ParseDirectory(child, trackStart);
                    dirNode.Children.Add(child);
                }

                pos += recordLen;
                if ((pos & 1) != 0) pos++;
            }
            if (!hasRecords) break;
        }
        return true;
    }

    private static bool CheckSig(byte[] d, int o, string s)
    {
        if (o + s.Length > d.Length) return false;
        for (int i = 0; i < s.Length; i++) if (d[o + i] != s[i]) return false;
        return true;
    }

    private static uint ReadCDiU32(byte[] p, int o)
    {
        uint be = BeU32(p, o + 4);
        if (be != 0) return be;
        uint beF = BeU32(p, o);
        if (beF != 0) return beF;
        return (uint)(p[o] | (p[o + 1] << 8) | (p[o + 2] << 16) | (p[o + 3] << 24));
    }

    private static uint BeU32(byte[] d, int o) => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
    private static ushort BeU16(byte[] d, uint o) => (ushort)((d[o] << 8) | d[o + 1]);
}
