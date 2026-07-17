using System.Text;
using CHDSharp;
using CHDSharp.Models;

namespace SimpleChdDrive.Core.Parsers;

public class SectorReader
{
    private readonly ChdFile _chd;
    private bool _trackLocked;

    private uint _cachedHunkNum = 0xFFFFFFFF;
    private byte[] _cachedHunk = [];
    private bool _cachedHunkSwapped;

    private bool _isOffsetDetected;
    private uint _offsetDetectedHunk = 0xFFFFFFFF;
    private TrackInfo? _offsetDetectedTrack;
    private readonly Dictionary<int, uint> _trackOffsetCache = [];

    private static readonly byte[] SyncPattern = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
    private const int SectorSize = 2048;

    public uint SectorHeaderOffset { get; private set; }

    public uint SyncOffset { get; private set; }

    public int LbaOffset { get; set; }

    public uint HunkBytes => _chd.HunkBytes;
    public uint UnitBytes { get; }

    public uint TotalBytes => (uint)_chd.TotalBytes;

    public SectorReader(ChdFile chd, uint unitBytes)
    {
        _chd = chd;
        UnitBytes = unitBytes;
        Tracks = ParseTracksWithLba(chd, unitBytes);
    }

    public void SetTrack(TrackInfo track, bool locked = false)
    {
        CurrentTrack = track;
        _trackLocked = locked;
    }

    public TrackInfo CurrentTrack { get; private set; } = null!;

    public List<TrackInfo> Tracks { get; }

    public void Reset()
    {
        _cachedHunkNum = 0xFFFFFFFF;
        _cachedHunk = [];
        _cachedHunkSwapped = false;
        _isOffsetDetected = false;
        SectorHeaderOffset = 0;
        SyncOffset = 0;
            _offsetDetectedHunk = 0xFFFFFFFF;
            _offsetDetectedTrack = null;
            LbaOffset = 0;
        _trackLocked = false;
        _trackOffsetCache.Clear();
    }

    public bool ReadSector(uint lba, byte[] outBuffer, int outOffset = 0)
    {
        if (!PrepareHunk(lba, out var rawOffset))
            return false;

        var sourceIndex = (int)(rawOffset + SectorHeaderOffset);
        if (sourceIndex + SectorSize > _cachedHunk.Length)
            return false;

        Array.Copy(_cachedHunk, sourceIndex, outBuffer, outOffset, SectorSize);
        return true;
    }

    public byte[]? ReadSector(uint lba)
    {
        var buffer = new byte[SectorSize];
        if (ReadSector(lba, buffer))
            return buffer;

        return null;
    }

    public bool ReadRawSector(uint lba, out byte[] rawSector)
    {
        rawSector = null!;
        if (!PrepareHunk(lba, out var rawOffset))
            return false;

        var unitBytes = (int)UnitBytes;
        if (rawOffset + unitBytes > _cachedHunk.Length)
            return false;

        rawSector = new byte[unitBytes];
        Array.Copy(_cachedHunk, (int)rawOffset, rawSector, 0, unitBytes);
        return true;
    }

    public byte GetSubheaderFileNumber(uint lba)
    {
        if (!PrepareHunk(lba, out var rawOffset))
            return 0xFF;

        var unitBytes = (int)UnitBytes;
        if (unitBytes < 2352)
            return 0xFF;

        var subheaderOffset = (int)rawOffset + 16;
        if (subheaderOffset + 1 > _cachedHunk.Length)
            return 0xFF;

        return _cachedHunk[subheaderOffset];
    }

    private bool PrepareHunk(uint lba, out uint rawOffsetInHunk)
    {
        rawOffsetInHunk = 0;

        var hunkBytes = _chd.HunkBytes;
        if (hunkBytes == 0 || UnitBytes == 0)
            return false;

        var sectorsPerHunk = hunkBytes / UnitBytes;
        if (sectorsPerHunk == 0)
            return false;

        uint chdFrame = 0;

        if (_trackLocked && CurrentTrack != null)
        {
            var relative = (long)lba - CurrentTrack.StartLba;
            if (relative >= 0 && relative < CurrentTrack.Frames)
            {
                chdFrame = CurrentTrack.ChdOffset + (uint)relative;
            }
            else
            {
                return false;
            }
        }
        else
        {
            var found = false;
            var adjustedLba = (uint)(lba + LbaOffset);

            if (CurrentTrack != null && adjustedLba >= CurrentTrack.StartLba &&
                adjustedLba < CurrentTrack.StartLba + CurrentTrack.Frames)
            {
                chdFrame = CurrentTrack.ChdOffset + (adjustedLba - CurrentTrack.StartLba);
                found = true;
            }

            if (!found && Tracks.Count > 0)
            {
                foreach (var track in Tracks)
                {
                    if (adjustedLba >= track.StartLba && adjustedLba < track.StartLba + track.Frames)
                    {
                        chdFrame = track.ChdOffset + (adjustedLba - track.StartLba);
                        CurrentTrack = track;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                chdFrame = adjustedLba;
                CurrentTrack = null!;
            }
        }

        var hunkNum = chdFrame / sectorsPerHunk;
        var sectorInHunk = chdFrame % sectorsPerHunk;

        var needsSwap = CurrentTrack?.TrackType == "AUDIO";

        if (hunkNum != _cachedHunkNum || _cachedHunkSwapped != needsSwap)
        {
            var buffer = new byte[hunkBytes];
            var err = _chd.ReadHunk(hunkNum, buffer);
            if (err != ChdError.Chderrnone)
                return false;

            if (needsSwap)
            {
                var swapped = new byte[hunkBytes];
                for (var i = 0; i + 1 < buffer.Length; i += 2)
                {
                    swapped[i] = buffer[i + 1];
                    swapped[i + 1] = buffer[i];
                }

                _cachedHunk = swapped;
            }
            else
            {
                _cachedHunk = buffer;
            }

            _cachedHunkNum = hunkNum;
            _cachedHunkSwapped = needsSwap;
            _isOffsetDetected = false;
        }

        rawOffsetInHunk = sectorInHunk * UnitBytes;

        var trackIdx = CurrentTrack?.Index ?? -1;
        if (!_isOffsetDetected || hunkNum != _offsetDetectedHunk || CurrentTrack != _offsetDetectedTrack)
        {
            if (_trackOffsetCache.TryGetValue(trackIdx, out var cachedOffset))
            {
                SectorHeaderOffset = cachedOffset;
                var isMode2 = CurrentTrack is not null &&
                              (CurrentTrack.TrackType.Contains("MODE2") || CurrentTrack.TrackType.Contains("CDI"));
                var headerSize = isMode2 ? 24u : 16u;
                SyncOffset = SectorHeaderOffset >= headerSize ? SectorHeaderOffset - headerSize : 0;
            }
            else if (UnitBytes >= 2352)
            {
                DetectSectorOffset(rawOffsetInHunk, trackIdx);
            }
            else
            {
                SyncOffset = 0;
                SectorHeaderOffset = 0;
                _trackOffsetCache[trackIdx] = SectorHeaderOffset;
            }

            _isOffsetDetected = true;
            _offsetDetectedHunk = hunkNum;
            _offsetDetectedTrack = CurrentTrack;
        }

        return true;
    }

    private void DetectSectorOffset(uint rawOffsetInHunk, int trackIdx)
    {
        var searchRange = (int)Math.Min(128, UnitBytes - 16);
        var foundSyncOffset = -1;

        for (var i = 0; i < searchRange; i++)
        {
            if ((int)(rawOffsetInHunk + i + 12) > _cachedHunk.Length)
                break;

            var match = true;
            for (var j = 0; j < 12; j++)
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
            var absoluteSync = rawOffsetInHunk + (uint)foundSyncOffset;
            SyncOffset = (uint)foundSyncOffset;
            SectorHeaderOffset = (uint)(foundSyncOffset + (_cachedHunk[absoluteSync + 15] == 2 ? 24 : 16));
            _trackOffsetCache[trackIdx] = SectorHeaderOffset;
        }
        else
        {
            var isCooked = false;
            uint cookedOffset = 0;
            uint[] candidateOffsets = [0, 16, 24, 96, 112, 120];

            foreach (var candidate in candidateOffsets)
            {
                if (rawOffsetInHunk + candidate + 16 > _cachedHunk.Length)
                    continue;

                var checkOff = (int)(rawOffsetInHunk + candidate);
                if (CheckSignature(checkOff, "CD001", 1) ||
                    CheckSignature(checkOff, "CDROM", 9) ||
                    CheckSignature(checkOff, "BE001", 1))
                {
                    isCooked = true;
                    cookedOffset = candidate;
                    break;
                }

                byte[] operaMagic = [0x01, 0x5A, 0x5A, 0x5A, 0x5A, 0x5A, 0x01];
                var operaMatch = true;
                for (var k = 0; k < 7; k++)
                {
                    if (_cachedHunk[checkOff + k] != operaMagic[k])
                    { operaMatch = false;
                        break; }
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
                SyncOffset = cookedOffset;
                SectorHeaderOffset = cookedOffset;
                _trackOffsetCache[trackIdx] = SectorHeaderOffset;
            }
            else
            {
                var fallbackOffset = GetSectorDataOffset(CurrentTrack);
                var fallbackIsZero = true;
                if (fallbackOffset > 0 && (int)(rawOffsetInHunk + fallbackOffset + 64) <= _cachedHunk.Length)
                {
                    for (var z = 0; z < 64; z++)
                    {
                        if (_cachedHunk[rawOffsetInHunk + fallbackOffset + z] != 0)
                        { fallbackIsZero = false;
                            break; }
                    }
                }

                if (fallbackIsZero)
                {
                    SyncOffset = 0;
                    SectorHeaderOffset = 0;
                }
                else
                {
                    SyncOffset = fallbackOffset;
                    SectorHeaderOffset = fallbackOffset;
                    _trackOffsetCache[trackIdx] = SectorHeaderOffset;
                }
            }
        }
    }

    private bool CheckSignature(int offset, string signature, int offsetInSignature)
    {
        var sig = Encoding.ASCII.GetBytes(signature);
        if (offset + offsetInSignature + sig.Length > _cachedHunk.Length)
            return false;

        for (var i = 0; i < sig.Length; i++)
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

    public static List<TrackInfo> ParseTracksWithLba(ChdFile chd, uint unitBytes)
    {
        var tracks = new List<TrackInfo>();
        var metadata = chd.Metadata;

        var hasTrackMetadata = false;

        foreach (var entry in metadata)
        {
            var tag = entry.Tag;
            if (tag.Length < 4) continue;

            var t0 = tag[0];
            var t1 = tag[1];
            var t2 = tag[2];
            var t3 = tag[3];

            var isTrackMeta = t0 == 'C' && t1 == 'H' &&
                              ((t2 == 'T' && t3 is '2' or 'R') || (t2 == 'G' && t3 is 'D' or 'T'));

            var metaText = entry.GetText();
            if (!isTrackMeta || string.IsNullOrEmpty(metaText))
                continue;

            hasTrackMetadata = true;
            var trackIndex = tracks.Count + 1;

            var parsed = TryParseTrackMetadata(metaText, out var typeStr,
                out var frames, out var pregap, out var postgap);

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
                Metadata = metaText
            });
        }

        if (!hasTrackMetadata || tracks.Count == 0)
            return tracks;

        var isGdrom = false;
        foreach (var entry in metadata)
        {
            var tag = entry.Tag;
            if (tag.Length < 4) continue;

            var t0 = tag[0];
            var t1 = tag[1];
            var t2 = tag[2];
            var t3 = tag[3];

            if (t0 == 'C' && t1 == 'H' && t2 == 'G' && t3 is 'D' or 'T')
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
                { isGdrom = true;
                    break; }
            }
        }

        if (!isGdrom && tracks.Count >= 3)
        {
            uint totalFrames = 0;
            foreach (var t in tracks)
            {
                totalFrames += t.Frames;
            }

            if (totalFrames is >= 500000 and <= 560000)
            {
                isGdrom = true;
            }
        }

        uint currentFileFrame = 0;
        uint currentLogicalLba = 150;
        const uint gdHighDensityLba = 45000;

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var trackNum = i + 1;

            if (isGdrom && trackNum >= 3 && currentLogicalLba < gdHighDensityLba)
            {
                currentLogicalLba = gdHighDensityLba;
            }

            track.StartLba = currentLogicalLba;
            track.ChdOffset = currentFileFrame;

            currentLogicalLba += track.Frames;
            var padded = (track.Frames + 3) / 4 * 4;
            currentFileFrame += padded;
        }

        foreach (var track in tracks)
        {
            if (!track.IsDataTrack && track is { TrackType: "AUDIO", Frames: > 16 } && unitBytes > 0)
            {
                var sectorsPerHunk = chd.HunkBytes / unitBytes;
                if (sectorsPerHunk > 0)
                {
                    var hunkNum = track.ChdOffset / sectorsPerHunk;
                    var offsetInHunk = track.ChdOffset % sectorsPerHunk * unitBytes;

                    var firstHunk = new byte[chd.HunkBytes];
                    if (chd.ReadHunk(hunkNum, firstHunk) == ChdError.Chderrnone &&
                        (int)(offsetInHunk + 12) <= firstHunk.Length)
                    {
                        var swappedSync = new byte[12];
                        for (var j = 0; j < 12; j++)
                        {
                            swappedSync[j] = firstHunk[offsetInHunk + (j ^ 1)];
                        }

                        var match = true;
                        for (var j = 0; j < 12; j++)
                        {
                            if (swappedSync[j] != SyncPattern[j])
                            { match = false;
                                break; }
                        }

                        if (match)
                        {
                            track.IsDataTrack = true;
                        }
                    }
                }
            }
        }

        return tracks;
    }

    private static bool TryParseTrackMetadata(string metadata, out string typeStr,
        out uint frames, out uint pregap, out uint postgap)
    {
        var parts = metadata.Split(' ');
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var colonIdx = part.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = part[..colonIdx];
                var value = part[(colonIdx + 1)..];
                map[key] = value;
            }
        }

        typeStr = map.GetValueOrDefault("TYPE", "");
        _ = uint.TryParse(map.GetValueOrDefault("FRAMES", "0"), out frames);
        _ = uint.TryParse(map.GetValueOrDefault("PREGAP", "0"), out pregap);
        _ = uint.TryParse(map.GetValueOrDefault("POSTGAP", "0"), out postgap);

        return frames > 0;
    }
}
