using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Models;
using NLog;

namespace kate.FileShare.Services;

public class UploadService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly ShortUrlService _shortUrlService;
    private readonly FileService _fileService;
    private readonly S3Service _s3;

    public UploadService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _fileService = services.GetRequiredService<FileService>();
        _s3 = services.GetRequiredService<S3Service>();
    }

    public const int ChunkLimit = 1024 * 1024;

    public async Task<FileModel> UploadBasicAsync(UserModel user, Stream stream, string filename, long length)
    {
        var fn = Path.GetFileName(filename) ?? "blob";
        var fileModel = new FileModel()
        {
            Filename = fn,
            MimeType = MimeTypes.GetMimeType(fn),
            Size = length,
            ShortUrl = _shortUrlService.Generate(),
            CreatedByUserId = user.Id,
        };
        fileModel.RelativeLocation = $"{fileModel.Id}/{fileModel.Filename}";

        using var ctx = _db.CreateSession();
        using var transaction = ctx.Database.BeginTransaction();
        try
        {
            Stream s3UploadSource = stream;
            string? tmpFilename = null;
            if (stream.Length != length)
            {
                tmpFilename = Path.GetTempFileName();
                using (var fs = File.Open(tmpFilename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await stream.CopyToAsync(fs);
                }
                s3UploadSource = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
            }

            var obj = await _s3.UploadObject(s3UploadSource, fileModel.RelativeLocation);
            fileModel.Size = obj.ContentLength;
            await _db.Files.AddAsync(fileModel);
            if (s3UploadSource != stream)
            {
                s3UploadSource.Dispose();
                if (!string.IsNullOrEmpty(tmpFilename))
                {
                    File.Delete(tmpFilename);
                }
            }
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error($"Failed to upload file for user {user.UserName} ({user.Id})\n{ex}");
            throw;
        }

        await _fileService.RecalculateSpaceUsed(user);
        return fileModel;
    }

    public async Task<ChunkUploadSessionModel> CreateSession(UserModel user, CreateSessionParams @params)
    {
        if (string.IsNullOrEmpty(@params.FileName))
        {
            throw new BadHttpRequestException($"Empty filename");
        }

        if (@params.ChunkSize > ChunkLimit)
        {
            throw new BadHttpRequestException(String.Format("Maximum chunk size is {0} bytes", ChunkLimit));
        }
        if (@params.ChunkSize < 1)
        {
            throw new BadHttpRequestException("Chunk size must be greater than zero");
        }
        if (@params.TotalSize < 1)
        {
            throw new BadHttpRequestException("Total size must be greater than zero");
        }

        using var ctx = _db.CreateSession();
        using var transaction = ctx.Database.BeginTransaction();
        try
        {
            var fileModel = new FileModel()
            {
                CreatedByUserId = user.Id
            };
            ctx.Files.Add(fileModel);

            var fileInfo = new S3FileInformationModel()
            {
                Id = fileModel.Id,
                FileSize = @params.TotalSize!.Value,
                ChunkSize = @params.ChunkSize!.Value
            };
            ctx.S3FileInformations.Add(fileInfo);

            var sessionModel = new ChunkUploadSessionModel()
            {
                FileId = fileModel.Id,
                UserId = user.Id
            };
            ctx.ChunkUploadSessions.Add(sessionModel);

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return sessionModel;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error($"Failed to create upload session for user {user.UserName} ({user.Id})\n{ex}");
            throw;
        }
    }
}