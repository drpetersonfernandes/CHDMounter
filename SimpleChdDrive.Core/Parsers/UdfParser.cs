using System.Text;

namespace SimpleChdDrive.Core.Parsers;

public class UdfParser
{
    private readonly SectorReader _reader;
    private uint _partitionStart;
    private uint _blockSize = 2048;

    public UdfParser(SectorReader reader)
    {
        _reader = reader;
    }

    public bool Parse(FsNode rootNode, TrackInfo? track = null)
    {
        if (track != null)
            _reader.SetTrack(track, true);
        var sector = new byte[2048];

        if (!_reader.ReadSector(256, sector)) return false;
        if (LeU16(sector, 0) != 2) return false; // AVDP tag

        var vdsLoc = LeU32(sector, 20);
        var vdsLen = LeU32(sector, 16);
        var vdsSectors = (vdsLen + _blockSize - 1) / _blockSize;

        uint fsdLoc = 0;
        _partitionStart = 0;

        for (uint i = 0; i < vdsSectors; i++)
        {
            if (!_reader.ReadSector(vdsLoc + i, sector)) break;

            var tagId = LeU16(sector, 0);
            if (tagId == 6) { fsdLoc = LeU32(sector, 304);
                _blockSize = LeU32(sector, 264); }
            else if (tagId == 5)
            {
                _partitionStart = LeU32(sector, 420);
            }
            else if (tagId == 8)
            {
                break;
            }
        }

        if (fsdLoc == 0) return false;

        if (!_reader.ReadSector(_partitionStart + fsdLoc, sector)) return false;
        if (LeU16(sector, 0) != 256) return false; // FSD

        rootNode.Name = "/";
        rootNode.IsDirectory = true;

        var rootIcbLba = LeU32(sector, 376);
        return ReadFileEntry(rootIcbLba, _partitionStart, rootNode);
    }

    private bool ReadFileEntry(uint logicalBlockNum, uint partitionStart, FsNode node)
    {
        var sector = new byte[2048];
        if (!_reader.ReadSector(partitionStart + logicalBlockNum, sector)) return false;

        var tagId = LeU16(sector, 0);
        ulong infoLength;
        byte[] allocDesc;
        ushort icbFlags;
        byte fileType;

        switch (tagId)
        {
            // File Entry
            case 261:
            {
                infoLength = LeU64(sector, 56);
                var lEa = LeU32(sector, 168);
                var lAd = LeU32(sector, 172);
                var baseOff = 176 + (int)lEa;
                if (baseOff > sector.Length) return false;

                allocDesc = new byte[lAd];
                Array.Copy(sector, baseOff, allocDesc, 0, Math.Min(lAd, (uint)(sector.Length - baseOff)));
                icbFlags = LeU16(sector, 52);
                fileType = sector[17];
                break;
            }
            // Extended File Entry
            case 266:
            {
                infoLength = LeU64(sector, 56);
                var lEa = LeU32(sector, 212);
                var lAd = LeU32(sector, 216);
                var baseOff = 220 + (int)lEa;
                if (baseOff > sector.Length) return false;

                allocDesc = new byte[lAd];
                Array.Copy(sector, baseOff, allocDesc, 0, Math.Min(lAd, (uint)(sector.Length - baseOff)));
                icbFlags = LeU16(sector, 52);
                fileType = sector[17];
                break;
            }
            default:
                return false;
        }

        node.Size = infoLength;
        node.IsDirectory = fileType == 4;
        node.Extents.Clear();

        uint adType = (ushort)(icbFlags & 0x0007);
        uint off = 0;

        switch (adType)
        {
            // Short ADs
            case 0:
            {
                while (off + 8 <= allocDesc.Length)
                {
                    var len = LeU32(allocDesc, (int)off);
                    var loc = LeU32(allocDesc, (int)(off + 4));
                    var type = len >> 30;
                    len &= 0x3FFFFFFF;
                    if (len > 0 && type == 0)
                    {
                        node.Extents.Add(new FsExtent { Lba = partitionStart + loc, Size = len });
                        if (node.Lba == 0)
                        {
                            node.Lba = partitionStart + loc;
                        }
                    }
                    off += 8;
                }

                break;
            }
            // Long ADs
            case 1:
            {
                while (off + 16 <= allocDesc.Length)
                {
                    var len = LeU32(allocDesc, (int)off);
                    var loc = LeU32(allocDesc, (int)(off + 4));
                    var type = len >> 30;
                    len &= 0x3FFFFFFF;
                    if (len > 0 && type == 0)
                    {
                        node.Extents.Add(new FsExtent { Lba = partitionStart + loc, Size = len });
                        if (node.Lba == 0)
                        {
                            node.Lba = partitionStart + loc;
                        }
                    }
                    off += 16;
                }

                break;
            }
        }

        if (node.IsDirectory)
            return ParseDirectory(node.Lba, partitionStart, node);

        return true;
    }

    private bool ParseDirectory(uint logicalBlockNum, uint partitionStart, FsNode dirNode)
    {
        foreach (var extent in dirNode.Extents)
        {
            var sectors = (uint)((extent.Size + _blockSize - 1) / _blockSize);
            for (uint s = 0; s < sectors; s++)
            {
                var sector = new byte[2048];
                if (!_reader.ReadSector(extent.Lba + s, sector)) break;

                uint pos = 0;
                while (pos + 38 <= sector.Length)
                {
                    var tagId = LeU16(sector, (int)pos);
                    if (tagId != 257) break; // FID

                    var fileChar = sector[pos + 18];
                    var nameLen = sector[pos + 19];
                    var implUseLen = LeU16(sector, (int)(pos + 36));

                    if (nameLen == 0) { pos += (uint)(38 + implUseLen + nameLen);
                        continue; }

                    var fidLen = 4u * ((38u + nameLen + implUseLen + 3u) / 4u);
                    if (fidLen == 0 || pos + fidLen > sector.Length) break;

                    var nameOffset = pos + 38 + implUseLen;
                    if (nameOffset + nameLen > sector.Length) break;

                    var name = ParseUdfName(sector, (int)nameOffset, nameLen);

                    if ((fileChar & 0x02) == 0) // Not parent
                    {
                        var icbLba = LeU32(sector, (int)(pos + 24));
                        var child = new FsNode { Name = name };
                        if (ReadFileEntry(icbLba, partitionStart, child))
                            dirNode.Children.Add(child);
                    }

                    pos += fidLen;
                }
            }
        }
        return true;
    }

    private static string ParseUdfName(byte[] data, int offset, int length)
    {
        if (length <= 1) return "";

        var compression = data[offset];
        switch (compression)
        {
            case 8:
                return Encoding.Latin1.GetString(data, offset + 1, length - 1).TrimEnd('\0');
            case 16:
            {
                var sb = new StringBuilder();
                for (var i = offset + 1; i + 1 < offset + length; i += 2)
                {
                    var u16 = (ushort)((data[i] << 8) | data[i + 1]);
                    if (u16 == 0) break;

                    sb.Append(char.ConvertFromUtf32(u16));
                }
                return sb.ToString();
            }
            default:
                return "";
        }
    }

    private static ushort LeU16(byte[] d, int o)
    {
        return (ushort)(d[o] | (d[o + 1] << 8));
    }

    private static uint LeU32(byte[] d, int o)
    {
        return (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
    }

    private static ulong LeU64(byte[] d, int o)
    {
        return d[o] | ((ulong)d[o + 1] << 8) | ((ulong)d[o + 2] << 16) | ((ulong)d[o + 3] << 24) |
               ((ulong)d[o + 4] << 32) | ((ulong)d[o + 5] << 40) | ((ulong)d[o + 6] << 48) | ((ulong)d[o + 7] << 56);
    }
}
