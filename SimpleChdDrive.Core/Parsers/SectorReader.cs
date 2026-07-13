namespace SimpleChdDrive.Core.Parsers;

public class SectorReader
{
    private readonly CHDFile _chd;
    private List<TrackInfo> _tracks = [];
    private TrackInfo _currentTrack;
    private int _lbaOffset;
    private bool _trackLocked;

    private uint _cachedHunkNum = 0xFFFFFFFF;
    private byte[] _cachedHunk = [];
    private bool _cachedHunkSwapped;

    private uint _sectorHeaderOffset;
    private bool _isOffsetDetected;
    private uint _offsetDetectedHunk = 0xFFFFFFFF;
    private TrackInfo _offsetDetectedTrack;
    private readonly Dictionary<int, uint> _trackOffsetCache = [];
    private uint _foundSyncOffset;

    private static readonly byte[] SyncPattern = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
    private const int SectorSize = 2048;

    public uint SectorHeaderOffset => _sectorHeaderOffset;
    public uint SyncOffset => _foundSyncOffset;
    public int LbaOffset { get => _lbaOffset; set => _lbaOffset = value; }
    public uint HunkBytes => _chd.HunkBytes;
    public uint UnitBytes => _chd.UnitBytes;
    public uint TotalBytes => (uint)_chd.TotalBytes;

    public SectorReader(CHDFile chd)
    {
        _chd = chd;
        _tracks = ParseTracksWithLBA(chd);
    }

    public void SetTrack(TrackInfo track, bool locked = false)
    {
        _currentTrack = track;
        _trackLocked = locked;
    }

    public TrackInfo CurrentTrack => _currentTrack;

    public List<TrackInfo> Tracks => _tracks;

    public void Reset()
    {
        _cachedHunkNum = 0xFFFFFFFF;
        _cachedHunk = [];
        _cachedHunkSwapped = false;
        _isOffsetDetected = false;
        _sectorHeaderOffset = 0;
        _foundSyncOffset = 0;
        _offsetDetectedHunk = 0xFFFFFFFF;
        _offsetDetectedTrack = null;
        _lbaOffset = 0;
        _trackLocked = false;
        _trackOffsetCache.Clear();
    }

    public bool ReadSector(uint lba, byte[] outBuffer, int outOffset = 0)
    {
        if (!PrepareHunk(lba, out uint rawOffset))
            return false;

        int sourceIndex = (int)(rawOffset + _sectorHeaderOffset);
        if (sourceIndex + SectorSize > _cachedHunk.Length)
            return false;

        Array.Copy(_cachedHunk, sourceIndex, outBuffer, outOffset, SectorSize);
        return true;
    }

    public byte[] ReadSector(uint lba)
    {
        var buffer = new byte[SectorSize];
        if (ReadSector(lba, buffer))
            return buffer;
        return null;
    }

    public bool ReadRawSector(uint lba, out byte[] rawSector)
    {
        rawSector = null;
        if (!PrepareHunk(lba, out uint rawOffset))
            return false;

        int unitBytes = (int)_chd.UnitBytes;
        if (rawOffset + unitBytes > _cachedHunk.Length)
            return false;

        rawSector = new byte[unitBytes];
        Array.Copy(_cachedHunk, (int)rawOffset, rawSector, 0, unitBytes);
        return true;
    }

    public byte GetSubheaderFileNumber(uint lba)
    {
        if (!PrepareHunk(lba, out uint rawOffset))
            return 0xFF;

        int unitBytes = (int)_chd.UnitBytes;
        if (unitBytes < 2352)
            return 0xFF;

        int subheaderOffset = (int)rawOffset + 16;
        if (subheaderOffset + 1 > _cachedHunk.Length)
            return 0xFF;

        return _cachedHunk[subheaderOffset];
    }

    private bool PrepareHunk(uint lba, out uint rawOffsetInHunk)
    {
        rawOffsetInHunk = 0;

        uint hunkBytes = _chd.HunkBytes;
        uint unitBytes = _chd.UnitBytes;
        if (hunkBytes == 0 || unitBytes == 0)
            return false;

        uint sectorsPerHunk = hunkBytes / unitBytes;
        uint chdFrame = 0;
        bool found = false;

        if (_trackLocked && _currentTrack != null)
        {
            long relative = (long)lba - _currentTrack.StartLBA;
            if (relative >= 0 && relative < _currentTrack.Frames)
            {
                chdFrame = _currentTrack.ChdOffset + (uint)relative;
                found = true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            uint adjustedLba = (uint)((long)lba + _lbaOffset);

            if (_currentTrack != null && adjustedLba >= _currentTrack.StartLBA &&
                adjustedLba < _currentTrack.StartLBA + _currentTrack.Frames)
            {
                chdFrame = _currentTrack.ChdOffset + (adjustedLba - _currentTrack.StartLBA);
                found = true;
            }

            if (!found && _tracks.Count > 0)
            {
                foreach (var track in _tracks)
                {
                    if (adjustedLba >= track.StartLBA && adjustedLba < track.StartLBA + track.Frames)
                    {
                        chdFrame = track.ChdOffset + (adjustedLba - track.StartLBA);
                        _currentTrack = track;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                chdFrame = adjustedLba;
                _currentTrack = null!;
            }
        }

        uint hunkNum = chdFrame / sectorsPerHunk;
        uint sectorInHunk = chdFrame % sectorsPerHunk;

        bool needsSwap = _currentTrack?.TrackType == "AUDIO";

        if (hunkNum != _cachedHunkNum || _cachedHunkSwapped != needsSwap)
        {
            var buffer = new byte[hunkBytes];
            var err = _chd.ReadHunk(hunkNum, buffer);
            if (err != chd_error.CHDERR_NONE)
                return false;

            if (needsSwap)
            {
                for (int i = 0; i + 1 < buffer.Length; i += 2)
                {
                    (buffer[i + 1], buffer[i]) = (buffer[i], buffer[i + 1]);
                }
            }

            _cachedHunk = buffer;
            _cachedHunkNum = hunkNum;
            _cachedHunkSwapped = needsSwap;
            _isOffsetDetected = false;
        }

        rawOffsetInHunk = sectorInHunk * unitBytes;

        int trackIdx = _currentTrack?.Index ?? -1;
        if (!_isOffsetDetected || hunkNum != _offsetDetectedHunk || _currentTrack != _offsetDetectedTrack)
        {
            if (_trackOffsetCache.TryGetValue(trackIdx, out uint cachedOffset))
            {
                _sectorHeaderOffset = cachedOffset;
                bool isMode2 = _currentTrack != null &&
                    (_currentTrack.TrackType.Contains("MODE2") || _currentTrack.TrackType.Contains("CDI"));
                uint headerSize = isMode2 ? 24u : 16u;
                _foundSyncOffset = _sectorHeaderOffset >= headerSize ? _sectorHeaderOffset - headerSize : 0;
            }
            else if (unitBytes >= 2352)
            {
                DetectSectorOffset(rawOffsetInHunk, trackIdx);
            }
            else
            {
                _foundSyncOffset = 0;
                _sectorHeaderOffset = 0;
                _trackOffsetCache[trackIdx] = _sectorHeaderOffset;
            }

            _isOffsetDetected = true;
            _offsetDetectedHunk = hunkNum;
            _offsetDetectedTrack = _currentTrack;
        }

        return true;
    }

    private void DetectSectorOffset(uint rawOffsetInHunk, int trackIdx)
    {
        uint unitBytes = _chd.UnitBytes;
        int searchRange = (int)Math.Min(128, unitBytes - 16);
        int foundSyncOffset = -1;

        for (int i = 0; i < searchRange; i++)
        {
            if ((int)(rawOffsetInHunk + i + 12) > _cachedHunk.Length)
                break;

            bool match = true;
            for (int j = 0; j < 12; j++)
            {
                if (_cachedHunk[rawOffsetInHunk + i + j] != SyncPattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                foundSyncOffset = i;
                break;
            }
        }

        if (foundSyncOffset >= 0)
        {
            uint absoluteSync = rawOffsetInHunk + (uint)foundSyncOffset;
            _foundSyncOffset = (uint)foundSyncOffset;
            _sectorHeaderOffset = (uint)(foundSyncOffset + (_cachedHunk[absoluteSync + 15] == 2 ? 24 : 16));
            _trackOffsetCache[trackIdx] = _sectorHeaderOffset;
        }
        else
        {
            bool isCooked = false;
            uint cookedOffset = 0;
            uint[] candidateOffsets = [0, 16, 24, 96, 112, 120];

            foreach (uint candidate in candidateOffsets)
            {
                if (rawOffsetInHunk + candidate + 16 > _cachedHunk.Length)
                    continue;

                int checkOff = (int)(rawOffsetInHunk + candidate);
                if (CheckSignature(checkOff, "CD001", 1) ||
                    CheckSignature(checkOff, "CDROM", 9) ||
                    CheckSignature(checkOff, "BE001", 1))
                {
                    isCooked = true;
                    cookedOffset = candidate;
                    break;
                }

                byte[] operaMagic = [0x01, 0x5A, 0x5A, 0x5A, 0x5A, 0x5A, 0x01];
                bool operaMatch = true;
                for (int k = 0; k < 7; k++)
                {
                    if (_cachedHunk[checkOff + k] != operaMagic[k])
                    { operaMatch = false; break; }
                }
                if (operaMatch)
                {
                    isCooked = true;
                    cookedOffset = candidate;
                    break;
                }
            }

            if (isCooked)
            {
                _foundSyncOffset = cookedOffset;
                _sectorHeaderOffset = cookedOffset;
                _trackOffsetCache[trackIdx] = _sectorHeaderOffset;
            }
            else
            {
                uint fallbackOffset = GetSectorDataOffset(_currentTrack);
                bool fallbackIsZero = true;
                if (fallbackOffset > 0 && (int)(rawOffsetInHunk + fallbackOffset + 64) <= _cachedHunk.Length)
                {
                    for (int z = 0; z < 64; z++)
                    {
                        if (_cachedHunk[rawOffsetInHunk + fallbackOffset + z] != 0)
                        { fallbackIsZero = false; break; }
                    }
                }

                if (fallbackIsZero)
                {
                    _foundSyncOffset = 0;
                    _sectorHeaderOffset = 0;
                }
                else
                {
                    _foundSyncOffset = fallbackOffset;
                    _sectorHeaderOffset = fallbackOffset;
                    _trackOffsetCache[trackIdx] = _sectorHeaderOffset;
                }
            }
        }
    }

    private bool CheckSignature(int offset, string signature, int offsetInSignature)
    {
        byte[] sig = System.Text.Encoding.ASCII.GetBytes(signature);
        if (offset + offsetInSignature + sig.Length > _cachedHunk.Length)
            return false;

        for (int i = 0; i < sig.Length; i++)
        {
            if (_cachedHunk[offset + offsetInSignature + i] != sig[i])
                return false;
        }
        return true;
    }

    public static uint GetSectorDataOffset(TrackInfo track)
    {
        if (track is null) return 16;
        if (!track.IsDataTrack) return 0;
        if (track.TrackType.Contains("MODE2") || track.TrackType.Contains("MODE_2")) return 24;
        if (track.TrackType.Contains("CDI")) return 24;
        return 16;
    }

    public static List<TrackInfo> ParseTracksWithLBA(CHDFile chd)
    {
        var tracks = new List<TrackInfo>();
        var metadata = chd.ReadMetadata();

        bool hasTrackMetadata = false;

        foreach (var entry in metadata)
        {
            uint tag = entry.Tag;
            char t0 = (char)((tag >> 24) & 0xFF);
            char t1 = (char)((tag >> 16) & 0xFF);
            char t2 = (char)((tag >> 8) & 0xFF);
            char t3 = (char)(tag & 0xFF);

            bool isTrackMeta = (t0 == 'C' && t1 == 'H' &&
                ((t2 == 'T' && (t3 == '2' || t3 == 'R')) || (t2 == 'G' && (t3 == 'D' || t3 == 'T'))));

            if (!isTrackMeta || string.IsNullOrEmpty(entry.Value))
                continue;

            hasTrackMetadata = true;
            int trackIndex = tracks.Count + 1;
            string metaValue = entry.Value;

            uint frames = 0;
            uint pregap = 0;
            uint postgap = 0;
            uint padFrames = 0;
            string typeStr = "";
            string subtypeStr = "";
            string pgtypeStr = "";
            string pgsubStr = "";
            int parsedTrackNum = 0;
            bool parsed = TryParseTrackMetadata(metaValue, ref parsedTrackNum, ref typeStr, ref subtypeStr,
                ref frames, ref padFrames, ref pregap, ref pgtypeStr, ref pgsubStr, ref postgap);

            if (!parsed || frames == 0)
                continue;

            tracks.Add(new TrackInfo
            {
                Index = trackIndex,
                Frames = frames,
                Pregap = pregap,
                Postgap = postgap,
                TrackType = typeStr,
                IsDataTrack = typeStr.Contains("MODE") || typeStr.Contains("CDI"),
                Metadata = metaValue
            });
        }

        if (!hasTrackMetadata || tracks.Count == 0)
            return tracks;

        bool isGdrom = false;
        foreach (var entry in metadata)
        {
            uint tag = entry.Tag;
            char t0 = (char)((tag >> 24) & 0xFF);
            char t1 = (char)((tag >> 16) & 0xFF);
            char t2 = (char)((tag >> 8) & 0xFF);
            char t3 = (char)(tag & 0xFF);

            if (t0 == 'C' && t1 == 'H' && t2 == 'G' && (t3 == 'D' || t3 == 'T'))
            {
                isGdrom = true;
                break;
            }
        }
        if (!isGdrom)
        {
            foreach (var t in tracks)
            {
                if (t.Metadata?.Contains("PAD:") == true)
                { isGdrom = true; break; }
            }
        }
        if (!isGdrom && tracks.Count >= 3)
        {
            uint totalFrames = 0;
            foreach (var t in tracks) totalFrames += t.Frames;
            if (totalFrames >= 500000 && totalFrames <= 560000)
                isGdrom = true;
        }

        uint currentFileFrame = 0;
        uint currentLogicalLBA = 150;
        const uint GdHighDensityLba = 45000;

        for (int i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            int trackNum = i + 1;

            if (isGdrom && trackNum >= 3 && currentLogicalLBA < GdHighDensityLba)
                currentLogicalLBA = GdHighDensityLba;

            track.StartLBA = currentLogicalLBA;
            track.ChdOffset = currentFileFrame;

            currentLogicalLBA += track.Frames;
            uint padded = (track.Frames + 3) / 4 * 4;
            currentFileFrame += padded;
        }

        foreach (var track in tracks)
        {
            if (!track.IsDataTrack && track.TrackType == "AUDIO" && track.Frames > 16 && chd.UnitBytes > 0)
            {
                uint sectorsPerHunk = chd.HunkBytes / chd.UnitBytes;
                if (sectorsPerHunk > 0)
                {
                    uint hunkNum = track.ChdOffset / sectorsPerHunk;
                    uint offsetInHunk = (track.ChdOffset % sectorsPerHunk) * chd.UnitBytes;

                    var firstHunk = new byte[chd.HunkBytes];
                    if (chd.ReadHunk(hunkNum, firstHunk) == chd_error.CHDERR_NONE &&
                        (int)(offsetInHunk + 12) <= firstHunk.Length)
                    {
                        byte[] swappedSync = new byte[12];
                        for (int j = 0; j < 12; j++)
                            swappedSync[j] = firstHunk[offsetInHunk + (j ^ 1)];

                        bool match = true;
                        for (int j = 0; j < 12; j++)
                        {
                            if (swappedSync[j] != SyncPattern[j])
                            { match = false; break; }
                        }
                        if (match)
                            track.IsDataTrack = true;
                    }
                }
            }
        }

        return tracks;
    }

    private static bool TryParseTrackMetadata(string metadata, ref int trackNum, ref string typeStr,
        ref string subtypeStr, ref uint frames, ref uint padFrames, ref uint pregap,
        ref string pgtypeStr, ref string pgsubStr, ref uint postgap)
    {
        var parts = metadata.Split(' ');
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            int colonIdx = part.IndexOf(':');
            if (colonIdx > 0)
            {
                string key = part[..colonIdx];
                string value = part[(colonIdx + 1)..];
                map[key] = value;
            }
        }

        typeStr = map.GetValueOrDefault("TYPE", "");
        subtypeStr = map.GetValueOrDefault("SUBTYPE", "");
        pgtypeStr = map.GetValueOrDefault("PGTYPE", "");
        pgsubStr = map.GetValueOrDefault("PGSUB", "");
        int.TryParse(map.GetValueOrDefault("TRACK", "0"), out trackNum);
        uint.TryParse(map.GetValueOrDefault("FRAMES", "0"), out frames);
        uint.TryParse(map.GetValueOrDefault("PREGAP", "0"), out pregap);
        uint.TryParse(map.GetValueOrDefault("POSTGAP", "0"), out postgap);
        uint.TryParse(map.GetValueOrDefault("PAD", "0"), out padFrames);

        return frames > 0;
    }
}
