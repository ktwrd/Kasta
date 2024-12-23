using ImageMagick;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using NLog;

namespace Kasta.Web.Services;

public class UploadService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly ShortUrlService _shortUrlService;
    private readonly FileService _fileService;
    private readonly S3Service _s3;
    private readonly PreviewService _previewService;

    public UploadService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _shortUrlService = services.GetRequiredService<ShortUrlService>();
        _fileService = services.GetRequiredService<FileService>();
        _s3 = services.GetRequiredService<S3Service>();
        _previewService = services.GetRequiredService<PreviewService>();
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
        using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var tmpFilename = Path.GetTempFileName();
            using (var fs = File.Open(tmpFilename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.CopyToAsync(fs);
            }
            var s3UploadSource = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);

            var obj = await _s3.UploadObject(s3UploadSource, fileModel.RelativeLocation);
            fileModel.Size = obj.ContentLength;
            await ctx.Files.AddAsync(fileModel);

            await ctx.SaveChangesAsync();
            await s3UploadSource.DisposeAsync();
            if (_previewService.PreviewSupported(fileModel))
            {
                await s3UploadSource.DisposeAsync();
                s3UploadSource = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                await _previewService.Create(ctx, fileModel, s3UploadSource);
            }

            using (var fstream = File.Open(tmpFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var imageInfo = GenerateFileImageInfo(fileModel, fstream);
                if (imageInfo != null)
                {
                    ctx.FileImageInfos.Add(imageInfo);
                    await ctx.SaveChangesAsync();
                }
            }

            File.Delete(tmpFilename);

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
    public FileImageInfoModel? GenerateFileImageInfo(FileModel file, Stream stream)
    {
        if (!(file.MimeType?.StartsWith("image/") ?? false)) return null;
        if (file.MimeType.Contains("svg")) return null;

        var info = new MagickImageInfo(stream);
        var model = new FileImageInfoModel()
        {
            Id = file.Id,
            Width = info.Width,
            Height = info.Height,
            ColorSpace = info.ColorSpace == ColorSpace.Undefined ? null : info.ColorSpace.ToString(),
            CompressionMethod = info.Compression == CompressionMethod.Undefined ? null : info.Compression.ToString(),
            MagickFormat = info.Format == MagickFormat.Unknown ? null : info.Format.ToString(),
            Interlace = info.Interlace == Interlace.Undefined ? null : info.Interlace.ToString(),
            CompressionLevel = info.Quality
        };
        return model;
    }
    public async Task<ChunkUploadSessionModel> CreateSession(UserModel user, CreateUploadSessionRequest @params)
    {
        if (string.IsNullOrEmpty(@params.Filename))
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