using Amazon.S3.Model;
using ImageMagick;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using Kasta.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Kasta.Web.Services;

public class FileService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3;
    private readonly AuditService _auditService;
    private readonly MailboxService _mailbox;
    public FileService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _s3 = services.GetRequiredService<S3Service>();
        _auditService = services.GetRequiredService<AuditService>();
        _mailbox = services.GetRequiredService<MailboxService>();
    }
    internal async Task<string?> DeleteFileInternal(ApplicationDbContext ctx, UserModel user, FileModel file)
    {
        await _auditService.InsertAuditData(ctx, _auditService.GenerateDeleteAudit(user, file, (e) => e.Id, FileModel.TableName));

        await _auditService.InsertAuditData(ctx, 
            _auditService.GenerateDeleteAudit(
                user,
                _db.ChunkUploadSessions.Where(e => e.FileId == file.Id),
                e => e.Id,
                ChunkUploadSessionModel.TableName));
        await _auditService.InsertAuditData(ctx, 
            _auditService.GenerateDeleteAudit(
                user,
                _db.S3FileChunks.Where(e => e.FileId == file.Id),
                e => e.Id,
                S3FileChunkModel.TableName));
        await _auditService.InsertAuditData(ctx, 
            _auditService.GenerateDeleteAudit(
                user,
                _db.S3FileInformations.Where(e => e.Id == file.Id),
                e => e.Id,
                S3FileInformationModel.TableName));
        await _auditService.InsertAuditData(
            ctx,
            _auditService.GenerateDeleteAudit(
                user,
                _db.FilePreviews.Where(e => e.Id == file.Id),
                e => e.Id,
                FilePreviewModel.TableName));
        await _auditService.InsertAuditData(
            ctx,
            _auditService.GenerateDeleteAudit(
                user,
                _db.FileImageInfos.Where(e => e.Id == file.Id),
                e => e.Id,
                FileImageInfoModel.TableName));

        var previewLocation = await ctx.FilePreviews.Where(e => e.Id == file.Id)
            .Select(e => e.RelativeLocation)
            .FirstOrDefaultAsync();

        await ctx.ChunkUploadSessions.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
        await ctx.S3FileChunks.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
        await ctx.S3FileInformations.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
        await ctx.FilePreviews.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
        await ctx.FileImageInfos.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
        await ctx.Files.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
        return previewLocation;
    }
    public async Task DeleteFile(UserModel user, FileModel file)
    {
        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        string? previewLocation = null;
        try
        {
            previewLocation = await DeleteFileInternal(ctx, user, file);

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to delete file {file.Id} ({file.RelativeLocation}) for user {user.UserName} ({user.Id})\n{ex}");
            await transaction.RollbackAsync();
            throw new ApplicationException(
                $"Failed to delete file {file.RelativeLocation} ({file.Id}) for user {user.UserName} ({user.Id})", ex);
        }

        if (previewLocation != null && previewLocation != file.RelativeLocation)
        {
            await _s3.DeleteObject(previewLocation);
        }
        await _s3.DeleteObject(file.RelativeLocation);
        if (file.CreatedByUser != null)
        {
            await RecalculateSpaceUsed(file.CreatedByUser);
        }
    }

    public async Task<long> RecalculateSpaceUsed(UserModel user)
    {
        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        long fileCount = 0;
        try
        {
            var totalSpaceQuery = ctx.Files.Where(e => e.CreatedByUserId == user.Id)
                .Include(e => e.Preview)
                .Select(e => e.Size + (e.Preview == null ? 0 : e.Preview.Size));
            var totalSpace = await totalSpaceQuery.SumAsync();
            fileCount = await totalSpaceQuery.LongCountAsync();
            var previewSpace = await ctx.Files
                .Where(e => e.CreatedByUserId == user.Id)
                .Where(e => e.Preview != null)
                .Include(e => e.Preview)
                .Select(e => e.Preview!.Size)
                .SumAsync();

            var limitModel = await ctx.UserLimits
                .FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (limitModel == null)
            {
                limitModel = new()
                {
                    UserId = user.Id
                };
                await ctx.UserLimits.AddAsync(limitModel);
            }
            limitModel.SpaceUsed = totalSpace;
            limitModel.PreviewSpaceUsed = previewSpace;
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to recalculate storage space for user: {ex}");
            await transaction.RollbackAsync();
            throw;
        }

        return fileCount;
    }

    /// <summary>
    /// Generate the metadata for all files.
    /// </summary>
    /// <param name="force">
    /// <para>
    /// When <see langword="false"/>, then only files that don't have a relation to <see cref="FileImageInfoModel"/> will be processed.
    /// </para>
    /// <b>Note</b>, this will take longer than just regenerating missing metadata.
    /// </param>
    /// <param name="userIdFilter">When the list has at least 1 item, then only files created by the specified user IDs will be included.</param>
    /// <param name="processOnDifferentThread">
    /// When <see langword="true"/>, then all the logic for generating the file metadata will be done on a new thread.
    /// </param>
    public async Task GenerateFileMetadata(
        bool force,
        List<string>? userIdFilter = null,
        bool processOnDifferentThread = true)
    {
        // get all images
        var files = await _db.Files.Where(e => e.MimeType != null && e.MimeType.StartsWith("image/")).ToListAsync();
        if (userIdFilter?.Count > 0)
        {
            files = files
                .Where(e => e.CreatedByUserId != null && userIdFilter.Contains(e.CreatedByUserId))
                .ToList();
        }

        // only include files that don't have FileImageInfo
        if (!force)
        {
            var fileIds = files.Select(e => e.Id).ToList();
            var imageInfoIds = await _db.FileImageInfos
                .Where(e => fileIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();
            files = files.Where(e => imageInfoIds.Contains(e.Id) == false).ToList();
        }

        if (files.Count < 1)
        {
            _log.Trace("No files to process :3");
            await _mailbox.CreateMessageAsync("Generate File Metadata - No Action Taken", [
                "No files could be found to generate metadata.",
            ]);
            return;
        }
        
        if (processOnDifferentThread)
        {
            var thread = new Thread(
                data =>
                {
                    if (!(data is List<FileModel> workingFiles))
                    {
                        _log.Error(
                            $"Failed to run thread since data has invalid type (expected List<FileModel>, got {data?.GetType()})");
                        return;
                    }

                    GenerateFileMetadataTask(workingFiles);
                });
            thread.Start(files);
        }
        else
        {
            GenerateFileMetadataTask(files);
        }
    }

    private void GenerateFileMetadataTask(List<FileModel> files)
    {
        if (files.Count < 1)
        {
            _log.Trace($"No files to process :3");
            _mailbox.CreateMessage("Generate File Metadata - No Action Taken", [
                "No files could be found to generate metadata.",
            ]);
            return;
        }
        var start = DateTimeOffset.UtcNow;
        try
        {
            int i = 0;
            int c = files.Count;
            Parallel.ForEach(files, file =>
            {
                try
                {
                    GenerateFileMetadataNow(file).Wait();
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        $"Failed to generate metadata for File {file.Id} ({file.Filename}, {file.MimeType}, {file.Size}b)",
                        ex);
                }
                finally
                {
                    _log.Debug($"Parallel {i}/{c}");
                    i++;
                }

            });
            
            var duration = DateTimeOffset.UtcNow - start;
            try
            {
                _log.Info("Creating mailbox message (for success)");
                _mailbox.CreateMessage("Generate File Metadata - Complete", [
                    $"Successfully generated metadata for {files.Count} file(s).",
                    $"Took `{duration}` (triggered at `{start}`)"
                ]);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to add message to system mailbox to notify that a task has finished.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to run task");
            SentrySdk.CaptureException(ex, scope =>
            {
                var fileIdList = files.Select(e => e.Id).ToList();
                scope.SetExtra("FileIdList", string.Join("\n", fileIdList));
                scope.SetExtra("TaskStartAt", start.ToUnixTimeSeconds());
                scope.SetTag("TaskName", nameof(GenerateFileMetadataTask));
            });
            try
            {
                var exceptionString = ex.ToString();
                _log.Info("Creating mailbox message (for failure)");
                _mailbox.CreateMessage(
                    "Generate File Metadata - Failure",
                    [
                        $"Failed to generate metadata for {files.Count} files.",
                        "```",
                        exceptionString.FancyMaxLength(SystemMailboxMessageModel.MessageMaxLength - 300),
                        "```",
                    ]);
            }
            catch (Exception iex)
            {
                _log.Error(iex, "Failed to report error in system inbox.");
            }
        }
    }

    public async Task GenerateFileMetadataNow(FileModel file, Logger? logger = null)
    {
        logger ??= LogManager.GetCurrentClassLogger();
        logger.Properties["FileId"] = file.Id;
        // file isn't an image, idc about metadata
        if (!(file.MimeType?.StartsWith("image/") ?? false))
            return;
        try
        {

            var res = await _s3.GetObject(file.RelativeLocation);
            var info = GenerateFileImageInfo(file, res.ResponseStream);
            if (info == null)
            {
                logger.Info($"Couldn't generate image info ({nameof(GenerateFileImageInfo)} returned null)");
                return;
            }

            await using (var ctx = _db.CreateSession())
            {
                var trans = await ctx.Database.BeginTransactionAsync();
                logger.Debug($"Created transaction ({trans.TransactionId})");

                try
                {
                    if (await ctx.FileImageInfos.AnyAsync(e => e.Id == info.Id))
                    {
                        var updateResult = await ctx.FileImageInfos
                            .Where(e => e.Id == info.Id)
                            .ExecuteUpdateAsync(e =>
                                e.SetProperty(x => x.Width, info.Width)
                                .SetProperty(x => x.Height, info.Height)
                                .SetProperty(x => x.ColorSpace, info.ColorSpace)
                                .SetProperty(x => x.CompressionMethod, info.CompressionMethod)
                                .SetProperty(x => x.MagickFormat, info.MagickFormat)
                                .SetProperty(x => x.Interlace, info.Interlace)
                                .SetProperty(x => x.CompressionLevel, info.CompressionLevel));
                        logger.Info($"Updated {updateResult} rows");
                    }
                    else
                    {
                        await ctx.FileImageInfos.AddAsync(info);
                        logger.Info($"Added 1 row");
                    }

                    await ctx.SaveChangesAsync();
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    logger.Error(ex, $"Failed to insert or update file info");
                    throw;
                }
            }
            res.Dispose();
            logger.Info("Done! Generated file metadata");
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Could not generate metadata for file {file.Id}", ex);
        }
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
    
    public MemoryStream GetMemoryStream(FileModel file, out GetObjectResponse res)
    {
        res = _s3.GetObject(file.RelativeLocation).Result;
        var ms = new MemoryStream();
        res.ResponseStream.CopyTo(ms);
        return ms;
    }
    public Stream GetStream(FileModel file, out GetObjectResponse res)
    {
        res = _s3.GetObject(file.RelativeLocation).Result;
        return res.ResponseStream;
    }

    public bool AllowPlaintextPreview(FileModel file)
    {
        if (file.Size > 524_288) return false;

        if (string.IsNullOrEmpty(file.MimeType))
            return false;
        
        var mimeWhitelist = new string[]
        {
            "application/json",
            "application/javascript",
            "application/xml",
            "application/xhtml+xml",
            "application/xhtml",
            "application/html",
            "application/toml",
            "application/sql",
            "application/postscript",
            "application/x-perl"
        };
        var mimeBlacklist = new string[]
        {
            "text/rtf",
            "text/richtext"
        };
        if (mimeBlacklist.Contains(file.MimeType))
            return false;
        if (file.MimeType?.StartsWith("text/") ?? false)
            return true;
        
        return mimeWhitelist.Contains(file.MimeType);
    }

    public string? GetPlaintextPreview(FileModel file)
    {
        var str = "";
        using var stream = GetStream(file, out var r);
        using var reader = new StreamReader(stream);
        str = reader.ReadToEnd();

        return str;
    }
}