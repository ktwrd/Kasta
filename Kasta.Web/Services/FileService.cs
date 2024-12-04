using Amazon.S3.Model;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Kasta.Web.Services;

public class FileService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3;
    private readonly AuditService _auditService;
    public FileService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _s3 = services.GetRequiredService<S3Service>();
        _auditService = services.GetRequiredService<AuditService>();
    }
    public async Task DeleteFile(UserModel user, FileModel file)
    {
        using var ctx = _db.CreateSession();
        using var transaction = await ctx.Database.BeginTransactionAsync();
        string? previewLocation = null;
        try
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

            previewLocation = await ctx.FilePreviews.Where(e => e.Id == file.Id).Select(e => e.RelativeLocation)
                .FirstOrDefaultAsync();

            await ctx.ChunkUploadSessions.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
            await ctx.S3FileChunks.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
            await ctx.S3FileInformations.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
            await ctx.FilePreviews.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
            await ctx.Files.Where(e => e.Id == file.Id).ExecuteDeleteAsync();

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

    public async Task RecalculateSpaceUsed(UserModel user)
    {
        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();

        try
        {
            var totalSpace = await ctx.Files.Where(e => e.CreatedByUserId == user.Id)
                .Include(e => e.Preview)
                .Select(e => e.Size + (e.Preview == null ? 0 : e.Preview.Size))
                .SumAsync();
            var previewSpace = await ctx.Files
                .Where(e => e.CreatedByUserId == user.Id)
                .Where(e => e.Preview != null)
                .Include(e => e.Preview)
                .Select(e => e.Preview!.Size)
                .SumAsync();

            var limitModel = await ctx.UserLimits.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
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
}