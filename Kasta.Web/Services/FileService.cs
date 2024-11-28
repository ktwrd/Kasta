using Amazon.S3.Model;
using Kasta.Web.Data;
using Kasta.Web.Data.Models;
using Kasta.Web.Data.Models.Audit;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Kasta.Web.Services;

public class FileService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3;
    public FileService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _s3 = services.GetRequiredService<S3Service>();
    }
    private List<(AuditModel, List<AuditEntryModel>)> GenerateDeleteAudit<T>(UserModel user, IEnumerable<T> data, Func<T, string> pkSelect, string tableName)
    {
        var result = new List<(AuditModel, List<AuditEntryModel>)>();
        foreach (var i in data)
        {
            result.Add(GenerateDeleteAudit(user, i, pkSelect, tableName));
        }
        return result;
    }
    private (AuditModel, List<AuditEntryModel>) GenerateDeleteAudit<T>(UserModel user, T obj, Func<T, string> pkSelect, string tableName)
    {
        var auditModel = new AuditModel()
        {
            CreatedBy = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Kind = AuditEventKind.Delete,
            EntityName = tableName,
            PrimaryKey = pkSelect(obj)
        };
        var entries = new List<AuditEntryModel>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var propTypeStr = prop.PropertyType.ToString();
            if (propTypeStr.StartsWith("System.Collections") || propTypeStr.StartsWith("Kasta.Web.Data.Models") || propTypeStr.StartsWith(nameof(NpgsqlTypes)))
                continue;
            var value = prop.GetValue(obj);

            string? stringValue = null;
            if (prop.PropertyType == typeof(string)
            || prop.PropertyType == typeof(char)

            || prop.PropertyType == typeof(sbyte)
            || prop.PropertyType == typeof(byte)
            || prop.PropertyType == typeof(short)
            || prop.PropertyType == typeof(ushort)
            || prop.PropertyType == typeof(int)
            || prop.PropertyType == typeof(uint)
            || prop.PropertyType == typeof(long)
            || prop.PropertyType == typeof(ulong)
            || prop.PropertyType == typeof(nint)
            || prop.PropertyType == typeof(nuint)

            || prop.PropertyType == typeof(float)
            || prop.PropertyType == typeof(double)
            || prop.PropertyType == typeof(decimal))
            {
                stringValue = value?.ToString();
            }
            else if (value is DateTime dt)
            {
                stringValue = dt.ToString("R");
            }
            else if (value is DateTimeOffset dto)
            {
                stringValue = dto.ToString("R");
            }
            else if (prop.PropertyType.IsEnum)
            {
                stringValue = value?.ToString();
            }
            else if (value == null)
            {
                stringValue = null;
            }
            else
            {
                throw new InvalidOperationException($"Unknown type {prop.PropertyType}");
            }
            entries.Add(new()
            {
                AuditId = auditModel.Id,
                PropertyName = prop.Name,
                Value = stringValue
            });
        }
    
        return (auditModel, entries);
    }
    private async Task InsertAuditData(ApplicationDbContext db, (AuditModel, List<AuditEntryModel>) data)
    {
        await db.Audit.AddAsync(data.Item1);
        await db.AuditEntries.AddRangeAsync(data.Item2.ToArray());
    }
    private async Task InsertAuditData(ApplicationDbContext db, List<(AuditModel, List<AuditEntryModel>)> data)
    {
        foreach (var i in data)
        {
            await InsertAuditData(db, i);
        }
    }
    public async Task DeleteFile(UserModel user, FileModel file)
    {
        using var ctx = _db.CreateSession();
        using var transaction = await ctx.Database.BeginTransactionAsync();
        string? previewLocation = null;
        try
        {
            await InsertAuditData(ctx, GenerateDeleteAudit(user, file, (e) => e.Id, FileModel.TableName));

            await InsertAuditData(ctx, 
                GenerateDeleteAudit(
                    user,
                    _db.ChunkUploadSessions.Where(e => e.FileId == file.Id),
                    e => e.Id,
                    ChunkUploadSessionModel.TableName));
            await InsertAuditData(ctx, 
                GenerateDeleteAudit(
                    user,
                    _db.S3FileChunks.Where(e => e.FileId == file.Id),
                    e => e.Id,
                    S3FileChunkModel.TableName));
            await InsertAuditData(ctx, 
                GenerateDeleteAudit(
                    user,
                    _db.S3FileInformations.Where(e => e.Id == file.Id),
                    e => e.Id,
                    S3FileInformationModel.TableName));
            await InsertAuditData(
                ctx,
                GenerateDeleteAudit(
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
            var files = await ctx.Files.Where(e => e.CreatedByUserId == user.Id)
                .Include(e => e.Preview)
                .Select(e => e.Size + (e.Preview == null ? 0 : e.Preview.Size))
                .ToListAsync();
            long size = 0;
            foreach (var i in files)
            {
                size += Math.Max(i, 0);
            }

            var limitModel = await ctx.UserLimits.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
            if (limitModel == null)
            {
                limitModel = new()
                {
                    UserId = user.Id
                };
                await ctx.UserLimits.AddAsync(limitModel);
            }
            limitModel.SpaceUsed = size;
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