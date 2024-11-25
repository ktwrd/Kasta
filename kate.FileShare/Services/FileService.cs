using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Data.Models.Audit;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace kate.FileShare.Services;

public class FileService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    public FileService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
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
            EntityName = tableName,
            PrimaryKey = pkSelect(obj)
        };
        var entries = new List<AuditEntryModel>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var propTypeStr = prop.PropertyType.ToString();
            if (propTypeStr.StartsWith("System.Collections") || propTypeStr.StartsWith("kate.FileShare.Data.Models"))
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
        using var transaction = ctx.Database.BeginTransaction();
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

            await ctx.ChunkUploadSessions.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
            await ctx.S3FileChunks.Where(e => e.FileId == file.Id).ExecuteDeleteAsync();
            await ctx.S3FileInformations.Where(e => e.Id == file.Id).ExecuteDeleteAsync();
            await ctx.Files.Where(e => e.Id == file.Id).ExecuteDeleteAsync();

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to delete file {file.Id} ({file.RelativeLocation}) for user {user.UserName} ({user.Id})\n{ex}");
            await transaction.RollbackAsync();
        }
    }

    public async Task RecalculateSpaceUsed(UserModel user)
    {
        using (var ctx = _db.CreateSession())
        {
            using var transaction = ctx.Database.BeginTransaction();

            try
            {
                var files = await ctx.Files.Where(e => e.CreatedByUserId == user.Id).Select(e => e.Size).ToListAsync();
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
    }
}