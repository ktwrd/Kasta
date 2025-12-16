using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models.Api.Request;
using NLog;

namespace Kasta.Web.Services;

public class UploadService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly ShortUrlService _shortUrlService;
    private readonly FileService _fileService;
    private readonly GenericFileService _genericFileService;
    private readonly PreviewService _previewService;
    private readonly KastaConfig _cfg;

    public UploadService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _fileService = services.GetRequiredService<FileService>();
        _genericFileService = services.GetRequiredService<GenericFileService>();
        _previewService = services.GetRequiredService<PreviewService>();
        _cfg = services.GetRequiredService<KastaConfig>();
    }

    public const int ChunkLimit = 1024 * 1024;

    public async Task<FileModel> UploadBasicAsync(UserModel user, Stream stream, string filename, long length)
    {
        var fn = Path.GetFileName(filename) ?? "blob";
        var id = Guid.NewGuid().ToString();
        var fileModel = new FileModel()
        {
            Id = id,
            Filename = fn,
            RelativeLocation = $"{id}/{fn}",
            MimeType = MimeTypes.GetMimeType(fn),
            Size = length,
            ShortUrl = _shortUrlService.Generate(),
            CreatedByUserId = user.Id,
        };

        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var tmpFilename = Path.GetTempFileName();
            await using (var fs = File.Open(tmpFilename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            await using (var fileStream = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await _genericFileService.UploadAsync(fileStream, fileModel.RelativeLocation);
                await fileStream.FlushAsync();
                fileModel.Size = (await _genericFileService.GetAsync(fileModel.RelativeLocation))?.Length ?? 0;
            }
            await ctx.Files.AddAsync(fileModel);


            await ctx.SaveChangesAsync();

            if (_previewService.PreviewSupported(fileModel))
            {
                using var other = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                await _previewService.Create(ctx, fileModel, other);
                await other.FlushAsync();
            }

            await using (var fileStream = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                var imageInfo = _fileService.GenerateFileImageInfo(fileModel, fileStream);
                if (imageInfo != null)
                {
                    await ctx.FileImageInfos.AddAsync(imageInfo);
                    await ctx.SaveChangesAsync();
                }
            }

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            File.Delete(tmpFilename);
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
    
    public async Task<ChunkUploadSessionModel> CreateSession(UserModel user, CreateUploadSessionRequest @params)
    {
        throw new NotImplementedException();
        /*if (string.IsNullOrEmpty(@params.Filename))
        {
            throw new BadHttpRequestException($"Empty filename");
        }

        if (@params.ChunkSize > ChunkLimit)
        {
            throw new BadHttpRequestException($"Maximum chunk size is {ChunkLimit} bytes");
        }
        if (@params.ChunkSize < 1)
        {
            throw new BadHttpRequestException("Chunk size must be greater than zero");
        }
        if (@params.TotalSize < 1)
        {
            throw new BadHttpRequestException("Total size must be greater than zero");
        }

        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var fileModel = new FileModel()
            {
                CreatedByUserId = user.Id
            };
            await ctx.Files.AddAsync(fileModel);

            var fileInfo = new S3FileInformationModel()
            {
                Id = fileModel.Id,
                FileSize = @params.TotalSize!.Value,
                ChunkSize = @params.ChunkSize!.Value
            };
            await ctx.S3FileInformations.AddAsync(fileInfo);

            var sessionModel = new ChunkUploadSessionModel()
            {
                FileId = fileModel.Id,
                UserId = user.Id
            };
            await ctx.ChunkUploadSessions.AddAsync(sessionModel);

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return sessionModel;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error($"Failed to create upload session for user {user.UserName} ({user.Id})\n{ex}");
            throw;
        }*/
    }
}