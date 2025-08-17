using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kasta.Data.Repositories;

public class GalleryRepository
{
    private readonly ApplicationDbContext _db;
    public GalleryRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }
    
    public async Task<GalleryModel?> GetById(
        string id)
    {
        id = id.Trim().ToLower();
        return await _db.Galleries
            .Include(e => e.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    
    public async Task<List<FileModel>> GetFilesForGallery(GalleryModel galleryRecord)
    {
        return  await _db.GalleryFileAssociations
            .Include(e => e.File)
            .Include(e => e.File.ImageInfo)
            .Include(e => e.File.Preview)
            .AsNoTracking()
            .Where(e => e.GalleryId == galleryRecord.Id)
            .OrderBy(e => e.SortOrder)
            .Select(e => e.File)
            .ToListAsync();
    }
    
    public async Task<GalleryModel> CreateDraftAsync(UserModel author)
    {
        var galleryModel = new GalleryModel()
        {
            IsDraft = true,
            Title = "",
            CreatedByUserId = author.Id,
        };
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.Galleries.AddAsync(galleryModel);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            throw new InvalidOperationException($"Failed to create draft for author \"{author.NormalizedUserName}\" ({author.Id})", ex);
        }
        return await _db.Galleries
            .Include(e => e.CreatedByUser)
            .AsNoTracking()
            .SingleAsync(e => e.Id == galleryModel.Id);
    }
    
    public async Task DeleteAsync(
        GalleryModel gallery,
        UserModel? deletor)
    {
        
    }
}