using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Fsp;
using Fsp.Interop;
using SimpleChdDrive.Core.Interfaces;
using VideoGameFileSystemParser.Parsers;
using FileInfo = Fsp.Interop.FileInfo;

namespace SimpleChdDrive_WinFsp;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal sealed class ChdFs : FileSystemBase, IDisposable, IAsyncDisposable
{
    private readonly ChdContainer _container;
    private readonly bool _persistentAcls;

    // ReSharper disable once UnusedParameter.Local
    public ChdFs(ChdContainer container, ILoggingService loggingService, bool persistentAcls = false)
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
            host.FileSystemName = _container.VolumeName;
            host.VolumeCreationTime = DateTimeToFileTimeUtc(DateTime.Now);
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

        NormalizedName = entry.Name;
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

        var entries = _container.ListDirectory(ResolvePath(FileNode)).ToList();
        var index = Context is int i ? i : 0;

        switch (index)
        {
            case 0:
                FileName = ".";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = 1;
                return true;
            case 1:
                FileName = "..";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = 2;
                return true;
        }

        var entryIndex = index - 2;
        if (entryIndex >= entries.Count)
            return false;

        var entry = entries[entryIndex];
        FileName = entry.Name;
        FileInfo = EntryToFileInfo(entry);
        Context = index + 1;
        return true;
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
        SecurityDescriptor = new byte[4096];
        SecurityDescriptor.Initialize();
        return STATUS_SUCCESS;
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
            IndexNumber = (ulong)entry.GetHashCode()
        };
    }

    private static string ResolvePath(object fileNode)
    {
        return fileNode is FileEntry e ? e.FullPath : "\\";
    }

    private static ulong DateTimeToFileTimeUtc(DateTime dateTime)
    {
        return (ulong)dateTime.ToFileTimeUtc();
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
