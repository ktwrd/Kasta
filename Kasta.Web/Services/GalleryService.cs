using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Services;

public class GalleryService
{
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3;
    private readonly AuditService _audit;
    private readonly FileService _fileService;
    private readonly ILogger<GalleryService> _log;
    public GalleryService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _s3 = services.GetRequiredService<S3Service>();
        _audit = services.GetRequiredService<AuditService>();
        _fileService = services.GetRequiredService<FileService>();
        _log = services.GetRequiredService<ILogger<GalleryService>>();
    }
    
    public async Task DeleteAsync(GalleryModel gallery, UserModel deletedBy)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        var deletedFiles = new List<FileModel>();
        var previewLocations = new List<string>();
        try
        {
            
            await _audit.InsertAuditData(
                ctx,
                _audit.GenerateDeleteAudit(deletedBy, gallery, e => e.Id, GalleryModel.TableName));
            await _audit.InsertAuditData(ctx,
                _audit.GenerateDeleteAudit(
                    deletedBy,
                    _db.GalleryFileAssociations.Where(e => e.GalleryId == gallery.Id),
                    e => e.FakeId,
                    GalleryFileAssociationModel.TableName));
            await _audit.InsertAuditData(ctx,
                _audit.GenerateDeleteAudit(
                    deletedBy,
                    _db.GalleryTextHistory.Where(e => e.GalleryId == gallery.Id),
                    e => e.FakeId,
                    GalleryTextHistoryModel.TableName));
            
            var files = await ctx.GalleryFileAssociations
                .Include(e => e.File)
                .AsNoTracking()
                .Where(e => e.GalleryId == gallery.Id)
                .Select(e => e.File)
                .ToListAsync();
            
            await ctx.GalleryTextHistory.Where(e => e.GalleryId == gallery.Id).ExecuteDeleteAsync();
            await ctx.GalleryFileAssociations.Where(e => e.GalleryId == gallery.Id).ExecuteDeleteAsync();
            await ctx.Galleries.Where(e => e.Id == gallery.Id).ExecuteDeleteAsync();
            
            foreach (var file in files)
            {
                var p = await _fileService.DeleteFileInternal(ctx, deletedBy, file);
                deletedFiles.Add(file);
                if (!string.IsNullOrEmpty(p))
                {
                    previewLocations.Add(p);
                }
            }
            
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Failed to delete gallery {GalleryId} for user {DeletedByUserName} ({DeletedById})",
                gallery.Id,
                deletedBy.UserName,
                deletedBy.Id);
            await trans.RollbackAsync();
            throw new ApplicationException(
                $"Failed to delete file {gallery.Id} for user {deletedBy.UserName} ({deletedBy.Id})", ex);
        }
        
        foreach (var file in deletedFiles)
        {
            _log.LogInformation("Deleting S3 Object: {FileRelativeLocation}", file.RelativeLocation);
            await _s3.DeleteObject(file.RelativeLocation);
        }
        foreach (var previewLocation in previewLocations)
        {
            _log.LogInformation("Deleting S3 Object: {PreviewLocation}", previewLocation);
            await _s3.DeleteObject(previewLocation);
        }
        if (gallery.CreatedByUser != null)
        {
            _log.LogInformation("Recalculating space for user: {GalleryCreatedByUserEmail}", gallery.CreatedByUser.Email);
            await _fileService.RecalculateSpaceUsed(gallery.CreatedByUser);
        }
    }
}