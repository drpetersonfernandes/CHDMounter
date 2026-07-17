using System.Security.AccessControl;
using System.Security.Principal;
using DokanNet;
using DokanFileAccess = DokanNet.FileAccess;

namespace SimpleChdDrive;

public class ChdFs : IDokanOperations, IDisposable
{
    private readonly ChdContainer _container;
    private readonly ILoggingService _loggingService;

    public ChdFs(ChdContainer container, ILoggingService loggingService)
    {
        _container = container;
        _loggingService = loggingService;
    }

    public NtStatus CreateFile(string fileName, DokanFileAccess access, FileShare share, FileMode mode,
        FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        if (fileName == "\\")
        {
            fileName = "\\";
        }

        var entry = _container.FindFile(fileName);
        if (entry == null)
            return DokanResult.PathNotFound;

        if (entry.IsDirectory)
        {
            info.IsDirectory = true;
            return mode switch
            {
                FileMode.Open or FileMode.OpenOrCreate => DokanResult.Success,
                FileMode.CreateNew => DokanResult.FileExists,
                _ => DokanResult.AccessDenied
            };
        }

        info.IsDirectory = false;

        if ((access & (DokanFileAccess.WriteData | DokanFileAccess.AppendData)) != 0)
            return DokanResult.AccessDenied;

        info.Context = entry;

        return mode switch
        {
            FileMode.Open or FileMode.OpenOrCreate => DokanResult.Success,
            FileMode.CreateNew => DokanResult.FileExists,
            _ => DokanResult.AccessDenied
        };
    }

    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        bytesRead = 0;

        if (info.IsDirectory)
            return DokanResult.AccessDenied;

        if (info.Context is not FileEntry entry)
        {
            entry = _container.FindFile(fileName);
            if (entry == null)
                return DokanResult.InvalidHandle;
        }

        bytesRead = _container.ReadFile(entry, (ulong)offset, buffer, 0, buffer.Length);
        return DokanResult.Success;
    }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        fileInfo = new FileInformation();

        var entry = _container.FindFile(fileName);
        if (entry == null)
            return DokanResult.PathNotFound;

        fileInfo.Attributes = entry.IsDirectory
            ? FileAttributes.Directory
            : FileAttributes.Archive | FileAttributes.ReadOnly;
        fileInfo.FileName = entry.Name;
        fileInfo.Length = (long)entry.Size;
        fileInfo.LastWriteTime = entry.ModifiedTime;
        fileInfo.CreationTime = entry.ModifiedTime;
        fileInfo.LastAccessTime = entry.ModifiedTime;

        info.IsDirectory = entry.IsDirectory;
        return DokanResult.Success;
    }

    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        var entries = _container.ListDirectory(fileName).ToList();

        if (entries.Count == 0)
        {
            var root = _container.FindFile(fileName);
            if (root == null)
            {
                files = Array.Empty<FileInformation>();
                return DokanResult.PathNotFound;
            }
        }

        var result = new List<FileInformation>
        {
            new() { FileName = ".", Attributes = FileAttributes.Directory },
            new() { FileName = "..", Attributes = FileAttributes.Directory }
        };

        foreach (var entry in entries)
        {
            result.Add(new FileInformation
            {
                FileName = entry.Name,
                Attributes = entry.IsDirectory ? FileAttributes.Directory : (FileAttributes.Archive | FileAttributes.ReadOnly),
                Length = (long)entry.Size,
                LastWriteTime = entry.ModifiedTime,
                CreationTime = entry.ModifiedTime,
                LastAccessTime = entry.ModifiedTime
            });
        }

        files = result;
        return DokanResult.Success;
    }

    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
    {
        var result = FindFiles(fileName, out var allFiles, info);
        if (result != DokanResult.Success)
        {
            files = Array.Empty<FileInformation>();
            return result;
        }

        if (searchPattern is "*" or "*.*")
        {
            files = allFiles;
            return DokanResult.Success;
        }

        files = allFiles.Where(f => WildcardMatch(f.FileName, searchPattern)).ToList();
        return DokanResult.Success;
    }

    private static bool WildcardMatch(string name, string pattern)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name,
                "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch { return false; }
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
        out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = _container.VolumeName;
        features = FileSystemFeatures.ReadOnlyVolume | FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk;
        fileSystemName = "CHDFS";
        maximumComponentLength = 255;
        return DokanResult.Success;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes,
        out long totalNumberOfFreeBytes, IDokanFileInfo info)
    {
        totalNumberOfBytes = (long)_container.VolumeSize;
        freeBytesAvailable = 0;
        totalNumberOfFreeBytes = 0;
        return DokanResult.Success;
    }

    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        _loggingService.Log($"Dokan mounted at {mountPoint}");
        return DokanResult.Success;
    }

    public NtStatus Unmounted(IDokanFileInfo info)
    {
        _loggingService.Log("Dokan unmounted");
        return DokanResult.Success;
    }

    public void Cleanup(string fileName, IDokanFileInfo info)
    {
    }

    public void CloseFile(string fileName, IDokanFileInfo info)
    {
        info.Context = null;
    }

    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        bytesWritten = 0;
        return DokanResult.AccessDenied;
    }

    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = Array.Empty<FileInformation>();
        return DokanResult.NotImplemented;
    }

    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
    {
        security = null!;
        try
        {
            var entry = _container.FindFile(fileName);
            var isDir = entry?.IsDirectory ?? (fileName == "\\");

            var everyoneSid = new SecurityIdentifier("S-1-1-0");

            if (isDir)
            {
                var ds = new DirectorySecurity();
                ds.AddAccessRule(new FileSystemAccessRule(everyoneSid, FileSystemRights.Read, AccessControlType.Allow));
                ds.SetOwner(everyoneSid);
                ds.SetGroup(everyoneSid);
                security = ds;
            }
            else
            {
                var fs = new FileSecurity();
                fs.AddAccessRule(new FileSystemAccessRule(everyoneSid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                fs.SetOwner(everyoneSid);
                fs.SetGroup(everyoneSid);
                security = fs;
            }

            return DokanResult.Success;
        }
        catch { return DokanResult.Error; }
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public void Dispose()
    {
        _container.Dispose();
        GC.SuppressFinalize(this);
    }
}
