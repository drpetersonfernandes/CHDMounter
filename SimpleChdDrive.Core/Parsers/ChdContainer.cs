using System.Globalization;
using System.Text;
using SimpleChdDrive.Core.CHD;

namespace SimpleChdDrive.Core.Parsers;

public class ChdContainer
{
    private const uint SectorSize = 2048;
    private const uint InvalidHandle = uint.MaxValue;

    private readonly List<FileEntry> _entries = [];
    private readonly List<uint> _parentHandles = [];
    private readonly Dictionary<string, uint> _entryMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SectorReader> _readerPool = [];
    private readonly ConcurrentBag<SectorReader> _availableReaders = [];
    private readonly object _poolLock = new();
    private bool _poolShutdown;
    private readonly string _chdPath;

    private ChdFile? _primaryChd;
    private bool _cueBinEnabled;
    private string _cueBinText = "";
    private ulong _cueBinSize;
    private uint _cueBinRawSectorSize;
    private string _cueBinStemName = "";
    private List<TrackInfo>? _cachedTracks;

    public string VolumeName { get; private set; } = "";
    public ulong VolumeSize { get; private set; }
    public uint UnitBytes { get; private set; }
    public uint HunkBytes { get; private set; }
    public ConsoleType ConsoleType { get; set; } = ConsoleType.Unknown;

    public ChdContainer(string chdPath)
    {
        _chdPath = chdPath;
    }

    public bool Open(ConsoleType consoleType)
    {
        ConsoleType = consoleType;

        var err = ChdFile.Open(_chdPath, out var chd);
        if (err != ChdError.Chderrnone)
            return false;

        _primaryChd = chd;

        var reader = new SectorReader(chd);
        UnitBytes = chd.UnitBytes;
        HunkBytes = chd.HunkBytes;
        VolumeSize = chd.TotalBytes;
        VolumeName = Path.GetFileNameWithoutExtension(_chdPath);

        _readerPool.Add(reader);
        _availableReaders.Add(reader);

        return true;
    }

    public bool MountAndParse(ConsoleType consoleType)
    {
        if (!Open(consoleType))
            return false;

        if (consoleType is ConsoleType.GenericCueBin or ConsoleType.GenericCueBin2048)
        {
            var rootNode = new FsNode { Name = "/", IsDirectory = true };
            BuildFromFsNode(rootNode);
            BuildVirtualCueBin(consoleType == ConsoleType.GenericCueBin2048);
            return true;
        }

        var parser = ParserFactory.CreateParser(consoleType, _readerPool[0]);
        if (parser is null)
            return false;

        var parsedRoot = new FsNode();
        if (!parser.Parse(parsedRoot))
            return false;

        BuildFromFsNode(parsedRoot);
        return true;
    }

    public void BuildFromFsNode(FsNode rootNode)
    {
        _entries.Clear();
        _parentHandles.Clear();
        _entryMap.Clear();

        var rootEntry = new FileEntry { Name = "\\", FullPath = "\\", Lba = rootNode.Lba, Size = rootNode.Size, IsDirectory = true };
        var rootHandle = RegisterEntry(rootEntry, InvalidHandle);

        foreach (var child in rootNode.Children)
            AddFsNodeRecursive(child, rootHandle, "\\");
    }

    private void AddFsNodeRecursive(FsNode node, uint parentHandle, string parentPath)
    {
        var currentPath = parentPath == "\\" ? $"\\{node.Name}" : $"{parentPath}\\{node.Name}";

        var entry = new FileEntry
        {
            Name = node.Name, FullPath = currentPath, Lba = node.Lba, Size = node.Size,
            IsDirectory = node.IsDirectory, FileNumber = node.FileNumber, IsInterleaved = node.IsInterleaved,
            IsRawPassthrough = node.IsRawPassthrough
        };

        foreach (var ext in node.Extents)
            entry.Extents.Add(new FileExtent { Lba = ext.Lba, Size = ext.Size });

        var handle = RegisterEntry(entry, parentHandle);

        if (entry.IsDirectory)
        {
            foreach (var child in node.Children)
                AddFsNodeRecursive(child, handle, currentPath);
        }
    }

    private uint RegisterEntry(FileEntry entry, uint parent)
    {
        _entries.Add(entry);
        _parentHandles.Add(parent);
        var handle = (uint)(_entries.Count - 1);
        _entryMap[ResolveEntryKey(handle)] = handle;
        return handle;
    }

    private string ResolveEntryKey(uint handle)
    {
        var parts = new List<string>();
        var current = handle;
        while (current != InvalidHandle)
        {
            parts.Add(_entries[(int)current].Name);
            current = _parentHandles[(int)current];
        }
        parts.Reverse();
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part == "\\") { sb.Append('\\'); }
            else { if (sb.Length > 0 && sb[^1] != '\\') sb.Append('\\');
                sb.Append(part); }
        }
        var path = sb.ToString().ToLowerInvariant();
        if (path.Length > 1 && path[^1] == '\\')
        {
            path = path[..^1];
        }

        if (string.IsNullOrEmpty(path))
        {
            path = "\\";
        }

        return path;
    }

    public FileEntry FindFile(string path)
    {
        var key = MakeEntryKey(path);
        return _entryMap.TryGetValue(key, out var handle) ? _entries[(int)handle] : null!;
    }

    private static string MakeEntryKey(string path)
    {
        if (string.IsNullOrEmpty(path) || path is "\\" or "/") return "\\";

        var result = path.Replace('/', '\\').ToLowerInvariant();
        if (result[0] != '\\')
        {
            result = '\\' + result;
        }

        while (result.Length > 1 && result[^1] == '\\')
        {
            result = result[..^1];
        }

        return result;
    }

    public IEnumerable<FileEntry> ListDirectory(string path)
    {
        var key = MakeEntryKey(path);
        if (!_entryMap.TryGetValue(key, out var handle)) yield break;

        for (uint i = 0; i < _parentHandles.Count; i++)
            if (_parentHandles[(int)i] == handle)
                yield return _entries[(int)i];
    }

    public int ReadFile(FileEntry entry, ulong offset, byte[] buffer, int bufOffset, int count)
    {
        if (entry.IsDirectory || offset >= entry.Size)
            return 0;

        var remaining = entry.Size - offset;
        var bytesToRead = (int)(remaining < (ulong)count ? remaining : (ulong)count);

        if (_cueBinEnabled)
        {
            var lowerName = entry.Name.ToLowerInvariant();
            if (lowerName == _cueBinStemName + ".cue")
            {
                if (offset >= (ulong)_cueBinText.Length) return 0;

                var cueRead = Math.Min(bytesToRead, _cueBinText.Length - (int)offset);
                Encoding.ASCII.GetBytes(_cueBinText, (int)offset, cueRead, buffer, bufOffset);
                return cueRead;
            }
            if (lowerName == _cueBinStemName + ".bin")
                return ReadVirtualBin(offset, buffer, bufOffset, bytesToRead);
        }

        if (entry.IsRawPassthrough)
        {
            return ReadRawChdBytes(offset, buffer, bufOffset, bytesToRead);
        }

        var reader = AcquireReader();
        if (reader == null) return 0;

        try
        {
            var totalRead = 0;
            if (!entry.IsInterleaved)
            {
                while (totalRead < bytesToRead)
                {
                    var curOff = offset + (ulong)totalRead;
                    var baseLba = entry.Lba;
                    var offsetInExtent = curOff;
                    if (entry.Extents.Count > 0)
                    {
                        ulong extentStart = 0;
                        foreach (var ext in entry.Extents)
                        {
                            if (curOff >= extentStart && curOff < extentStart + ext.Size)
                            { baseLba = ext.Lba;
                                offsetInExtent = curOff - extentStart;
                                break; }
                            extentStart += ext.Size;
                        }
                    }
                    var secNum = baseLba + (uint)(offsetInExtent / SectorSize);
                    var secOff = (uint)(offsetInExtent % SectorSize);
                    var sec = new byte[SectorSize];
                    if (!reader.ReadSector(secNum, sec)) break;

                    var chunk = Math.Min((int)(SectorSize - secOff), bytesToRead - totalRead);
                    Array.Copy(sec, (int)secOff, buffer, bufOffset + totalRead, chunk);
                    totalRead += chunk;
                }
            }
            else
            {
                var psec = entry.Lba;
                uint scanned = 0;
                while (totalRead < bytesToRead && scanned < 500000)
                {
                    scanned++;
                    var fn = reader.GetSubheaderFileNumber(psec);
                    psec++;
                    if (fn != entry.FileNumber) continue;

                    var sec = new byte[SectorSize];
                    if (!reader.ReadSector(psec - 1, sec)) break;

                    var toCopy = Math.Min((int)SectorSize, bytesToRead - totalRead);
                    Array.Copy(sec, 0, buffer, bufOffset + totalRead, toCopy);
                    totalRead += toCopy;
                }
            }
            return totalRead;
        }
        finally { ReleaseReader(reader); }
    }

    private void BuildVirtualCueBin(bool cooked2048)
    {
        _cachedTracks = SectorReader.ParseTracksWithLba(_primaryChd!);
        if (_cachedTracks.Count == 0) return;

        _cueBinEnabled = true;
        _cueBinStemName = Path.GetFileNameWithoutExtension(_chdPath);

        var rawSize = cooked2048 ? 2048u : Math.Min(_primaryChd!.UnitBytes, 2352u);
        _cueBinRawSectorSize = rawSize;

        uint cumulativeFrames = 0;
        _cueBinSize = 0;
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"FILE \"{_cueBinStemName}.bin\" BINARY");

        var trackNum = 0;
        foreach (var t in _cachedTracks)
        {
            trackNum++;
            var mode = t.IsDataTrack
                ? t.TrackType.Contains("MODE2") || t.TrackType.Contains("CDI") ? $"MODE2/{rawSize}" : $"MODE1/{rawSize}"
                : "AUDIO";

            sb.AppendLine(CultureInfo.InvariantCulture, $"  TRACK {trackNum:D2} {mode}");

            if (t.Pregap > 0)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    INDEX 00 {SectorToMsf(cumulativeFrames)}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    INDEX 01 {SectorToMsf(cumulativeFrames + t.Pregap)}");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    INDEX 01 {SectorToMsf(cumulativeFrames)}");
            }

            cumulativeFrames += t.Frames;
            _cueBinSize += (ulong)t.Frames * rawSize;
        }

        _cueBinText = sb.ToString();

        FileEntry cueEntry = new()
        {
            Name = _cueBinStemName + ".cue",
            Lba = 0,
            Size = (ulong)_cueBinText.Length,
            IsDirectory = false
        };
        RegisterEntry(cueEntry, 0);

        FileEntry binEntry = new()
        {
            Name = _cueBinStemName + ".bin",
            Lba = 0,
            Size = _cueBinSize,
            IsDirectory = false
        };
        RegisterEntry(binEntry, 0);
    }

    private static string SectorToMsf(uint sectors)
    {
        var m = sectors / (75 * 60);
        var s = sectors / 75 % 60;
        var f = sectors % 75;
        return $"{m:D2}:{s:D2}:{f:D2}";
    }

    private int ReadVirtualBin(ulong offset, byte[] buffer, int bufOffset, int bytesToRead)
    {
        if (_cachedTracks == null || _cachedTracks.Count == 0) return 0;

        var reader = AcquireReader();
        if (reader == null) return 0;

        try
        {
            var currentOffset = offset;
            var totalRead = 0;

            while (totalRead < bytesToRead)
            {
                ulong cumulative = 0;
                TrackInfo? targetTrack = null;
                ulong trackByteOffset = 0;

                foreach (var t in _cachedTracks)
                {
                    var trackBytes = (ulong)t.Frames * _cueBinRawSectorSize;
                    if (currentOffset >= cumulative && currentOffset < cumulative + trackBytes)
                    {
                        targetTrack = t;
                        trackByteOffset = cumulative;
                        break;
                    }
                    cumulative += trackBytes;
                }

                if (targetTrack == null) break;

                reader.SetTrack(targetTrack, true);
                var offsetInTrack = currentOffset - trackByteOffset;
                var frameInTrack = (uint)(offsetInTrack / _cueBinRawSectorSize);
                var byteInFrame = (uint)(offsetInTrack % _cueBinRawSectorSize);
                var logicalLba = targetTrack.StartLba + frameInTrack;

                if (reader.ReadRawSector(logicalLba, out var rawSector) && rawSector != null)
                {
                    var dataOffset = _cueBinRawSectorSize == 2048 ? reader.SectorHeaderOffset : reader.SyncOffset;
                    var available = (int)(_cueBinRawSectorSize - byteInFrame);
                    var toCopy = Math.Min(available, bytesToRead - totalRead);

                    if (dataOffset + byteInFrame + toCopy <= rawSector.Length)
                        Array.Copy(rawSector, dataOffset + byteInFrame, buffer, bufOffset + totalRead, toCopy);
                    else
                        Array.Clear(buffer, bufOffset + totalRead, toCopy);

                    totalRead += toCopy;
                    currentOffset += (uint)toCopy;
                }
                else
                {
                    break;
                }
            }
            return totalRead;
        }
        finally { ReleaseReader(reader); }
    }

    private int ReadRawChdBytes(ulong offset, byte[] buffer, int bufOffset, int bytesToRead)
    {
        if (_primaryChd == null) return 0;

        var err = _primaryChd.Read(offset, buffer, bufOffset, bytesToRead);
        return err == ChdError.Chderrnone ? bytesToRead : 0;
    }

    private SectorReader AcquireReader()
    {
        lock (_poolLock)
        {
            if (_poolShutdown) return null!;
            if (_availableReaders.TryTake(out var reader)) return reader;
        }
        return _readerPool.Count > 0 ? _readerPool[0] : null!;
    }

    private void ReleaseReader(SectorReader reader)
    {
        lock (_poolLock) { _availableReaders.Add(reader); }
    }

    public void Dispose()
    {
        lock (_poolLock) { _poolShutdown = true; }
        _readerPool.Clear();
        _availableReaders.Clear();
        _cachedTracks = null;
        _primaryChd?.Dispose();
    }
}
