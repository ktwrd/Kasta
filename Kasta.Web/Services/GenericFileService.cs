using Amazon.S3.Model;
using Kasta.Shared;

namespace Kasta.Web.Services;

public class GenericFileService
{
    private readonly S3Service _s3;
    private readonly KastaConfig _cfg;
    private readonly SystemSettingsProxy _systemSettings;
    public GenericFileService(IServiceProvider services)
    {
        _s3 = services.GetRequiredService<S3Service>();
        _systemSettings = services.GetRequiredService<SystemSettingsProxy>();
        _cfg = services.GetRequiredService<KastaConfig>();
    }

    public async Task<GenericFileInfo?> GetAsync(string location)
    {
        if (_cfg.LocalFileStorage.Enabled)
        {
            var parsedLocation = ParseLocation(location, out var exists);
            if (!exists) return null;
            return new GenericFileInfo(new FileInfo(parsedLocation));
        }
        else
        {
            try
            {
                var obj = await _s3.GetObject(location);
                if (obj == null) return null;
                return new GenericFileInfo(obj);
            }
            catch (S3ObjectNotFoundException)
            {
                return null;
            }
        }
    }

    public async Task<Stream?> GetStreamAsync(string location)
    {
        if (_cfg.LocalFileStorage.Enabled)
        {
            var parsedLocation = ParseLocation(location, out var exists);
            if (!exists) return null;
            // mimeType = MimeTypes.GetMimeType(Path.GetFileName(parsedLocation));
            return File.Open(parsedLocation, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        else
        {
            try
            {
                var obj = await _s3.GetObject(location);
                return obj?.ResponseStream;
            }
            catch (S3ObjectNotFoundException)
            {
                return null;
            }
        }
    }

    public async Task UploadAsync(Stream stream, string location)
    {
        if (_cfg.LocalFileStorage.Enabled)
        {
            var parsedLocation = ParseLocation(location, out var _);
            using var file = File.Open(parsedLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            file.SetLength(0);
            file.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(file);
        }
        else
        {
            await _s3.UploadObject(stream, location);
        }
    }

    public async Task DeleteAsync(string location)
    {
        if (_cfg.LocalFileStorage.Enabled)
        {
            var parsedLocation = ParseLocation(location, out var exists);
            if (!exists) return;
            File.Delete(parsedLocation);
        }
        else
        {
            await _s3.DeleteObject(location);
        }
    }

    public async Task<string?> PresignedUrlAsync(string location, TimeSpan duration)
    {
        if (_cfg.LocalFileStorage.Enabled) return null;
        if (!_systemSettings.S3UsePresignedUrl) return null;
        return await _s3.GeneratePresignedUrl(location, duration);
    }


    private string ParseLocation(string inputLocation, out bool exists)
    {
        if (string.IsNullOrEmpty(_cfg.LocalFileStorage.Directory?.Trim()))
        {
            throw new InvalidOperationException("Local File Storage Directory not properly configured!");
        }
        var location = Path.Combine(_cfg.LocalFileStorage.Directory, inputLocation);
        if (!location.StartsWith(_cfg.LocalFileStorage.Directory))
        {
            throw new Exception($"Location attempted to escape\n" +
                $"{nameof(location)}: {location}\n" +
                $"_cfg.LocalFileStorage.Directory: {_cfg.LocalFileStorage.Directory})");
        }
        exists = File.Exists(location);
        var locationParent = Path.GetDirectoryName(location);
        if (locationParent != null && !Directory.Exists(locationParent)) Directory.CreateDirectory(locationParent);
        return location;
    }

    public class GenericFileInfo
    {
        public GenericFileInfo()
        {
            Name = "";
            Length = 0;
        }
        public GenericFileInfo(FileInfo info)
        {
            DirectoryName = info.DirectoryName;
            Directory = info.Directory == null ? null : new GenericDirectoryInfo(info.Directory);
            Exists = info.Exists;
            IsReadOnly = info.IsReadOnly;
            Length = info.Length;
            Name = info.Name;
            ModifiedAt = info.Exists ? File.GetLastWriteTimeUtc(info.FullName) : null;
        }
        public GenericFileInfo(GetObjectResponse response)
        {
            ModifiedAt = response.LastModified.HasValue ? new DateTimeOffset(response.LastModified.Value.ToUniversalTime()) : null;
        }
        public string? DirectoryName { get; }
        public GenericDirectoryInfo? Directory { get; }
        public bool Exists { get; }
        public bool IsReadOnly { get; }
        public long Length { get; }
        public string Name { get; }
        public DateTimeOffset? ModifiedAt { get; }
    }
    public class GenericDirectoryInfo
    {
        public GenericDirectoryInfo()
        {
            Name = "";
        }
        public GenericDirectoryInfo(DirectoryInfo info)
        {
            Exists = info.Exists;
            Name = info.Name;
        }

        public bool Exists { get; }
        public string Name { get; }
    }
}