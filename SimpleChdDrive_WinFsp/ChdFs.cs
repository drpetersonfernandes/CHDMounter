using System.Buffers;
using System.Runtime.InteropServices;
using Fsp;
using Fsp.Interop;
using FileInfo = Fsp.Interop.FileInfo;

namespace SimpleChdDrive_WinFsp;

public sealed class ChdFs : FileSystemBase, IDisposable
{
    private readonly ChdContainer _container;
    private readonly ILoggingService _loggingService;

    public ChdFs(ChdContainer container, ILoggingService loggingService)
    {
        _container = container;
        _loggingService = loggingService;
    }

    public override int Init(object host2)
    {
        if (host2 is FileSystemHost host)
        {
            host.CasePreservedNames = true;
            host.UnicodeOnDisk = true;
            host.PostCleanupWhenModifiedOnly = true;
            host.FlushAndPurgeOnCleanup = true;
            host.FileSystemName = _container.VolumeName;
            host.VolumeCreationTime = DateTimeToFileTimeUtc(DateTime.Now);
            host.VolumeSerialNumber = (uint)Environment.TickCount;
        }
        return STATUS_SUCCESS;
    }

    public override int Open(string fileName, uint createOptions, uint grantedAccess,
        out object fileNode, out object fileDesc, out FileInfo fileInfo, out string normalizedName)
    {
        return OpenOrCreate(fileName, out fileNode, out fileDesc, out fileInfo, out normalizedName);
    }

    public override void Close(object fileNode, object fileDesc) { }

    private int OpenOrCreate(string fileName, out object fileNode, out object fileDesc,
        out FileInfo fileInfo, out string normalizedName)
    {
        fileNode = null!;
        fileDesc = null!;
        fileInfo = default;
        normalizedName = fileName;

        var entry = _container.FindFile(fileName);
        if (entry == null)
        {
            if (fileName is "\\" or "/")
            {
                entry = new FileEntry { Name = "\\", IsDirectory = true };
            }
            else
                return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        normalizedName = entry.Name;
        fileNode = entry;
        fileDesc = entry;
        fileInfo = EntryToFileInfo(entry);
        return STATUS_SUCCESS;
    }

    public override int Read(object fileNode, object fileDesc, IntPtr buffer, ulong offset,
        uint length, out uint bytesTransferred)
    {
        bytesTransferred = 0;

        if (fileNode is FileEntry { IsDirectory: true })
            return STATUS_ACCESS_DENIED;

        if (fileNode is not FileEntry entry)
            return STATUS_INVALID_HANDLE;

        var readBuffer = ArrayPool<byte>.Shared.Rent((int)length);
        try
        {
            var read = _container.ReadFile(entry, offset, readBuffer, 0, (int)length);
            if (read > 0)
                Marshal.Copy(readBuffer, 0, buffer, read);
            bytesTransferred = (uint)read;
            return STATUS_SUCCESS;
        }
        finally { ArrayPool<byte>.Shared.Return(readBuffer); }
    }

    public override int GetFileInfo(object fileNode, object fileDesc, out FileInfo fileInfo)
    {
        if (fileNode is FileEntry entry)
        {
            fileInfo = EntryToFileInfo(entry);
            return STATUS_SUCCESS;
        }
        fileInfo = default;
        return STATUS_UNSUCCESSFUL;
    }

    public override int GetDirInfoByName(object fileNode, object fileDesc, string fileName,
        out string normalizedName, out FileInfo fileInfo)
    {
        normalizedName = fileName;
        fileInfo = default;

        if (fileNode is not FileEntry { IsDirectory: true })
            return STATUS_OBJECT_NAME_NOT_FOUND;

        foreach (var child in _container.ListDirectory(ResolvePath(fileNode)))
        {
            if (string.Equals(child.Name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                normalizedName = child.Name;
                fileInfo = EntryToFileInfo(child);
                return STATUS_SUCCESS;
            }
        }
        return STATUS_OBJECT_NAME_NOT_FOUND;
    }

    public override bool ReadDirectoryEntry(object fileNode, object fileDesc, string pattern,
        string marker, ref object context, out string fileName, out FileInfo fileInfo)
    {
        fileName = null!;
        fileInfo = default;

        if (fileNode is not FileEntry { IsDirectory: true })
            return false;

        var entries = _container.ListDirectory(ResolvePath(fileNode)).ToList();
        var index = context is int i ? i : 0;

        switch (index)
        {
            case 0:
                fileName = ".";
                fileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                context = 1;
                return true;
            case 1:
                fileName = "..";
                fileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                context = 2;
                return true;
        }

        var entryIndex = index - 2;
        if (entryIndex >= entries.Count)
            return false;

        var entry = entries[entryIndex];
        fileName = entry.Name;
        fileInfo = EntryToFileInfo(entry);
        context = index + 1;
        return true;
    }

    public override int GetVolumeInfo(out VolumeInfo volumeInfo)
    {
        volumeInfo = default;
        volumeInfo.TotalSize = _container.VolumeSize;
        volumeInfo.FreeSize = 0;
        return STATUS_SUCCESS;
    }

    public override int GetSecurityByName(string fileName, out uint fileAttributes,
        ref byte[] securityDescriptor)
    {
        var entry = _container.FindFile(fileName);
        fileAttributes = (uint)(entry?.IsDirectory == true
            ? FileAttributes.Directory
            : FileAttributes.Archive | FileAttributes.ReadOnly);
        securityDescriptor = new byte[4096];
        securityDescriptor.Initialize();
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
}
