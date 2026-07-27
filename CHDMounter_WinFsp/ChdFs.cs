using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Fsp;
using Fsp.Interop;
using VideoGameFileSystemParser.Parsers;
using FileInfo = Fsp.Interop.FileInfo;

namespace CHDMounter_WinFsp;

/// <summary>
/// Implements the WinFsp file system interface to expose a CHD container as a read-only virtual drive.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal sealed class ChdFs : FileSystemBase, IDisposable, IAsyncDisposable
{
    private readonly ChdContainer _container;
    private readonly bool _persistentAcls;
    private static long _nextIndexNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChdFs"/> class.
    /// </summary>
    /// <param name="container">The parsed CHD container to serve files from.</param>
    /// <param name="persistentAcls">If <c>true</c>, enables persistent ACL support for cross-integrity mounts.</param>
    public ChdFs(ChdContainer container, bool persistentAcls = false)
    {
        _container = container;
        _persistentAcls = persistentAcls;
    }

    public override int Init(object Host)
    {
        if (Host is FileSystemHost host)
        {
            host.CasePreservedNames = true;
            host.UnicodeOnDisk = true;
            host.PersistentAcls = _persistentAcls;
            host.PostCleanupWhenModifiedOnly = true;
            host.FlushAndPurgeOnCleanup = true;
            host.PassQueryDirectoryPattern = true;
            host.MaxComponentLength = 255;
            host.SectorSize = 2048;
            host.FileSystemName = _container.VolumeName;
            host.VolumeCreationTime = DateTimeToFileTimeUtc(DateTime.UtcNow);
            host.VolumeSerialNumber = (uint)Environment.TickCount;
        }

        return STATUS_SUCCESS;
    }

    public override int Open(string FileName, uint CreateOptions, uint GrantedAccess,
        out object FileNode, out object FileDesc, out FileInfo FileInfo, out string NormalizedName)
    {
        return OpenOrCreate(FileName, out FileNode, out FileDesc, out FileInfo, out NormalizedName);
    }

    public override void Close(object FileNode, object FileDesc)
    {
    }

    private int OpenOrCreate(string FileName, out object FileNode, out object FileDesc,
        out FileInfo FileInfo, out string NormalizedName)
    {
        FileNode = null!;
        FileDesc = null!;
        FileInfo = default;
        NormalizedName = FileName;

        var entry = _container.FindFile(FileName);
        if (entry is null)
            return STATUS_OBJECT_NAME_NOT_FOUND;

        NormalizedName = entry.FullPath;
        FileNode = entry;
        FileDesc = entry;
        FileInfo = EntryToFileInfo(entry);
        return STATUS_SUCCESS;
    }

    public override int Read(object FileNode, object FileDesc, IntPtr Buffer, ulong Offset,
        uint Length, out uint BytesTransferred)
    {
        BytesTransferred = 0;

        if (FileNode is FileEntry { IsDirectory: true })
            return STATUS_ACCESS_DENIED;

        if (FileNode is not FileEntry entry)
            return STATUS_INVALID_HANDLE;

        var readBuffer = ArrayPool<byte>.Shared.Rent((int)Length);
        try
        {
            var read = _container.ReadFile(entry, Offset, readBuffer, 0, (int)Length);
            if (read > 0)
                Marshal.Copy(readBuffer, 0, Buffer, read);
            BytesTransferred = (uint)read;
            return STATUS_SUCCESS;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    public override int GetFileInfo(object FileNode, object FileDesc, out FileInfo FileInfo)
    {
        if (FileNode is FileEntry entry)
        {
            FileInfo = EntryToFileInfo(entry);
            return STATUS_SUCCESS;
        }

        FileInfo = default;
        return STATUS_UNSUCCESSFUL;
    }

    public override int GetDirInfoByName(object FileNode, object FileDesc, string FileName,
        out string NormalizedName, out FileInfo FileInfo)
    {
        NormalizedName = FileName;
        FileInfo = default;

        if (FileNode is not FileEntry { IsDirectory: true })
            return STATUS_OBJECT_NAME_NOT_FOUND;

        foreach (var child in _container.ListDirectory(ResolvePath(FileNode)))
        {
            if (string.Equals(child.Name, FileName, StringComparison.OrdinalIgnoreCase))
            {
                NormalizedName = child.Name;
                FileInfo = EntryToFileInfo(child);
                return STATUS_SUCCESS;
            }
        }

        return STATUS_OBJECT_NAME_NOT_FOUND;
    }

    public override bool ReadDirectoryEntry(object FileNode, object FileDesc, string Pattern,
        string Marker, ref object Context, out string FileName, out FileInfo FileInfo)
    {
        FileName = null!;
        FileInfo = default;

        if (FileNode is not FileEntry { IsDirectory: true })
            return false;

        List<FileEntry> entries;
        int index;

        if (Context is (List<FileEntry> cached, int cachedIndex))
        {
            entries = cached;
            index = cachedIndex;
        }
        else
        {
            entries = _container.ListDirectory(ResolvePath(FileNode)).ToList();
            index = 0;
        }

        switch (index)
        {
            case 0:
                FileName = ".";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = (entries, 1);
                return true;
            case 1:
                FileName = "..";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = (entries, 2);
                return true;
        }

        while (true)
        {
            var entryIndex = index - 2;
            if (entryIndex >= entries.Count)
                return false;

            var entry = entries[entryIndex];
            index++;
            Context = (entries, index);

            if (!string.IsNullOrEmpty(Pattern) && Pattern != "*" && Pattern != "*.*")
            {
                if (!MatchesPattern(entry.Name, Pattern))
                    continue;
            }

            FileName = entry.Name;
            FileInfo = EntryToFileInfo(entry);
            return true;
        }
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        var nameSpan = name.AsSpan();
        var patternSpan = pattern.AsSpan();
        return MatchesPatternRecursive(nameSpan, patternSpan);
    }

    private static bool MatchesPatternRecursive(ReadOnlySpan<char> name, ReadOnlySpan<char> pattern)
    {
        while (pattern.Length > 0)
        {
            if (pattern[0] == '*')
            {
                pattern = pattern[1..];
                if (pattern.Length == 0)
                    return true;

                for (var i = 0; i <= name.Length; i++)
                {
                    if (MatchesPatternRecursive(name[i..], pattern))
                        return true;
                }
                return false;
            }

            if (name.Length == 0)
                return false;

            if (pattern[0] == '?' || char.ToUpperInvariant(pattern[0]) == char.ToUpperInvariant(name[0]))
            {
                name = name[1..];
                pattern = pattern[1..];
            }
            else
            {
                return false;
            }
        }

        return name.Length == 0;
    }

    public override int GetVolumeInfo(out VolumeInfo VolumeInfo)
    {
        VolumeInfo = default;
        VolumeInfo.TotalSize = _container.VolumeSize;
        VolumeInfo.FreeSize = 0;
        return STATUS_SUCCESS;
    }

    public override int GetSecurityByName(string FileName, out uint FileAttributes,
        ref byte[] SecurityDescriptor)
    {
        var entry = _container.FindFile(FileName);
        if (entry is null)
        {
            FileAttributes = 0;
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        FileAttributes = (uint)(entry.IsDirectory
            ? System.IO.FileAttributes.Directory
            : System.IO.FileAttributes.Archive | System.IO.FileAttributes.ReadOnly);

        if (_persistentAcls)
        {
            var sd = CreateDefaultSecurityDescriptor();
            if (SecurityDescriptor == null || SecurityDescriptor.Length < sd.Length)
            {
                SecurityDescriptor = new byte[sd.Length];
            }

            Array.Copy(sd, SecurityDescriptor, sd.Length);
        }

        return STATUS_SUCCESS;
    }

    private byte[]? _cachedSecurityDescriptor;

    private byte[] CreateDefaultSecurityDescriptor()
    {
        if (_cachedSecurityDescriptor != null)
            return _cachedSecurityDescriptor;

        const string sddl = "D:P(A;;FA;;;WD)";
        var sd = new RawSecurityDescriptor(sddl);
        var bytes = new byte[sd.BinaryLength];
        sd.GetBinaryForm(bytes, 0);
        _cachedSecurityDescriptor = bytes;
        return bytes;
    }

    private static FileInfo EntryToFileInfo(FileEntry entry)
    {
        return new FileInfo
        {
            FileAttributes = (uint)(entry.IsDirectory ? FileAttributes.Directory : FileAttributes.Archive | FileAttributes.ReadOnly),
            FileSize = entry.Size,
            AllocationSize = entry.Size,
            CreationTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            LastAccessTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            LastWriteTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            ChangeTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            IndexNumber = (ulong)Interlocked.Increment(ref _nextIndexNumber)
        };
    }

    private static string ResolvePath(object fileNode)
    {
        return fileNode is FileEntry e ? e.FullPath : "\\";
    }

    private static ulong DateTimeToFileTimeUtc(DateTime dateTime)
    {
        try
        {
            return (ulong)dateTime.ToFileTimeUtc();
        }
        catch (ArgumentOutOfRangeException)
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _container.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _container.Dispose();
        return ValueTask.CompletedTask;
    }
}
