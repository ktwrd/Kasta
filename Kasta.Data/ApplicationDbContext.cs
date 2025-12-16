using System.Runtime.CompilerServices;
using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel>, IDataProtectionKeyContext
{
    private readonly DbContextOptions<ApplicationDbContext> _ops;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _ops = options;
    }

    public ApplicationDbContext CreateSession()
    {
        return new(_ops);
    }

    public void EnsureInitialRoles()
    {
        var rolesExist = Database.SqlQuery<object>(FormattableStringFactory.Create("SELECT FROM information_schema.tables WHERE table_name = 'AspNetRoles'")).Any();
        if (!rolesExist)
        {
            return;
        }
        var trans = Database.BeginTransaction();
        try
        {
            foreach (var item in RoleKind.ToList())
            {
                if (!Roles.Any(e => e.Name == item.Name))
                {
                    Roles.Add(new IdentityRole()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = item.Name,
                        NormalizedName = item.Name.ToUpper(),
                        ConcurrencyStamp = null
                    });
                }
            }
            SaveChanges();
            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }

    }

    #region IDataProtectionKeyContext
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    #endregion
    public DbSet<UserApiKeyModel> UserApiKeys { get; set; }
    public DbSet<UserSettingModel> UserSettings { get; set; }
    public DbSet<UserLimitModel> UserLimits { get; set; }
    public DbSet<PreferencesModel> Preferences { get; set; } // TODO rename to SystemSettings
    public DbSet<FileModel> Files { get; set; }
    public DbSet<FileImageInfoModel> FileImageInfos { get; set; }
    public DbSet<FilePreviewModel> FilePreviews { get; set; }
    public DbSet<S3FileInformationModel> S3FileInformations { get; set; }
    public DbSet<S3FileChunkModel> S3FileChunks { get; set; }
    public DbSet<ChunkUploadSessionModel> ChunkUploadSessions { get; set; } 
    public DbSet<ShortLinkModel> ShortLinks { get; set; }

    public DbSet<AuditModel> Audit { get; set; }
    public DbSet<AuditEntryModel> AuditEntries { get; set; }

    public DbSet<TrustedProxyHeaderMappingModel> TrustedProxyHeaderMappings { get; set; }
    public DbSet<TrustedProxyHeaderModel> TrustedProxyHeaders { get; set; }
    public DbSet<TrustedProxyModel> TrustedProxies { get; set; }
    
    public DbSet<SystemMailboxMessageModel> SystemMailboxMessages { get; set; }

    public List<T> Paginate<T>(IQueryable<T> query, int page, int pageSize, out bool lastPage)
    {
        var count = query.Count();
        var lastPageIndex = Convert.ToInt32(Math.Ceiling(count / (double)pageSize));
        int skip = 0;
        if (page > 1)
        {
            skip = (page - 1) * pageSize;
        }

        var result = query
            .Skip(skip)
            .Take(pageSize)
            .ToList();
        
        lastPage = result.Count < pageSize || page >= lastPageIndex;

        return result;
    }
    public async Task<(List<T> results, bool lastPage)> PaginateAsync<T>(IQueryable<T> query, int page, int pageSize)
    {
        var count = await query.CountAsync();
        var lastPageIndex = Convert.ToInt32(Math.Ceiling(count / (double)pageSize));
        int skip = 0;
        if (page > 1)
        {
            skip = (page - 1) * pageSize;
        }

        var result = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
        
        var lastPage = result.Count < pageSize || page >= lastPageIndex;

        return (result, lastPage);
    }

    public IQueryable<FileModel> SearchFiles(string? query, string? userId = null, bool asNoTracking = false)
    {
        var queryable = Files.AsQueryable();
        if (asNoTracking)
            queryable = queryable.AsNoTracking();
        if (!string.IsNullOrEmpty(userId))
        {
            queryable = queryable.Where(e => e.CreatedByUserId == userId);
        }
        if (!string.IsNullOrEmpty(query))
        {
            queryable = queryable.Where(e => e.SearchVector.Matches(query) || e.Filename.StartsWith(query) || e.Filename.EndsWith(query));
        }
        return queryable;
    }

    public async Task<FileModel?> GetFileAsync(string id, bool includeAuthor = false, bool includePreview = false, bool includeImageInfo = false, bool asNoTracking = false)
    {
        var query = (asNoTracking ? Files.AsNoTracking() : Files).Where(e => e.Id == id || e.ShortUrl == id);
        if (includeAuthor)
        {
            query = query.Include(e => e.CreatedByUser);
        }
        if (includePreview)
        {
            query = query.Include(e => e.Preview);
        }
        if (includeImageInfo)
        {
            query = query.Include(e => e.ImageInfo);
        }
        var target = await query.FirstOrDefaultAsync();
        return target;
    }
    public async Task<bool> FileExistsAsync(string id)
    {
        return await Files.AnyAsync(e => e.Id == id || e.ShortUrl == id);
    }

    public async Task<List<FileModel>> GetFilesCreatedBy(UserModel user, bool asNoTracking = false)
    {
        var result = await (asNoTracking ? Files.AsNoTracking() : Files)
            .Where(e => e.CreatedByUserId == user.Id)
            .Include(e => e.CreatedByUser)
            .Include(e => e.Preview)
            .Include(e => e.ImageInfo)
            .ToListAsync();
        return result;
    }
    public FileModel? GetFile(string id, bool asNoTracking = false)
    {
        var q = (asNoTracking ? Files.AsNoTracking() : Files);
        var target = q.Where(e => e.Id == id).Include(e => e.Preview).FirstOrDefault();
        if (target == null)
        {
            target = q.Where(e => e.ShortUrl == id).Include(e => e.Preview).FirstOrDefault();
        }
        return target;
    }
    public UserSettingModel GetUserSettings(UserModel user)
    {
        var r = UserSettings
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == user.Id);
        if (r == null)
        {
            using var ctx = CreateSession();
            using var transaction = ctx.Database.BeginTransaction();
            try
            {
                r = new UserSettingModel()
                {
                    Id = user.Id
                };
                ctx.UserSettings.Add(r);
                ctx.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        return r;
    }
    public async Task<UserSettingModel> GetUserSettingsAsync(UserModel user)
    {
        var r = await UserSettings.FirstOrDefaultAsync(e => e.Id == user.Id);
        if (r == null)
        {
            await using var ctx = CreateSession();
            await using var transaction = await ctx.Database.BeginTransactionAsync();
            try
            {
                r = new UserSettingModel()
                {
                    Id = user.Id
                };
                await ctx.UserSettings.AddAsync(r);
                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        return r;
    }
    public async Task<UserModel?> GetUserAsync(string id)
    {
        var item = await Users.Where(e => e.Id == id)
            .Include(e => e.Limit)
            .Include(e => e.ApiKeys)
            .Include(e => e.Settings)
            .FirstOrDefaultAsync();
        return item;
    }
    public async Task<bool> UserExistsAsync(string id)
    {
        return await Users.Where(e => e.Id == id).AnyAsync();
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditModel>()
            .ToTable(AuditModel.TableName)
            .HasKey(b => b.Id);
        builder.Entity<AuditEntryModel>()
            .ToTable(AuditEntryModel.TableName)
            .HasKey(b => b.Id);


        builder.Entity<UserLimitModel>()
            .ToTable(UserLimitModel.TableName)
            .HasKey(e => e.UserId);
        builder.Entity<UserSettingModel>()
            .ToTable(UserSettingModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<PreferencesModel>()
            .ToTable(PreferencesModel.TableName)
            .HasKey(b => b.Key);

        builder.Entity<UserModel>(
            b =>
            {
                b.HasOne(e => e.Limit)
                    .WithOne(e => e.User)
                    .HasForeignKey<UserLimitModel>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
                b.HasOne(e => e.Settings)
                    .WithOne(e => e.User)
                    .HasForeignKey<UserSettingModel>(e => e.Id)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
                b.HasMany(e => e.ApiKeys)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(true);
            });
        builder.Entity<FileImageInfoModel>(b =>
        {
            b.ToTable(FileImageInfoModel.TableName);
            b.HasKey(e => e.Id);
        });
        builder.Entity<FileModel>(
            b =>
            {
                b.ToTable(FileModel.TableName);
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
                b.HasIndex(e => e.Filename).IsUnique(false);
                b.HasIndex(e => e.MimeType).IsUnique(false);

                b.HasGeneratedTsVectorColumn(
                        p => p.SearchVector, "english", p => new
                        {
                            p.Filename,
                            p.MimeType,
                            p.ShortUrl
                        })
                    .HasIndex(p => p.SearchVector).HasMethod("GIN");

                b.HasOne(e => e.CreatedByUser)
                    .WithOne()
                    .HasForeignKey<FileModel>(e => e.CreatedByUserId)
                    .IsRequired(false);
                b.HasOne(e => e.ImageInfo)
                    .WithOne(e => e.File)
                    .HasForeignKey<FileImageInfoModel>(e => e.Id)
                    .IsRequired(false);
                
                b.HasOne(e => e.Preview)
                    .WithOne(e => e.File)
                    .HasForeignKey<FilePreviewModel>(e => e.Id)
                    .IsRequired(false);
                
                b.HasOne(e => e.S3FileInformation)
                    .WithOne(e => e.File)
                    .HasForeignKey<S3FileInformationModel>(e => e.Id)
                    .IsRequired(false);
            });
        builder.Entity<ShortLinkModel>(b =>
        {
            b.ToTable(ShortLinkModel.TableName);
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
            b.HasIndex(e => e.ShortLink).IsUnique(true);

            b.HasOne(e => e.CreatedByUser)
                .WithOne()
                .HasForeignKey<ShortLinkModel>(e => e.CreatedByUserId)
                .IsRequired(false);
        });
        builder.Entity<UserApiKeyModel>(b =>
        {
            b.ToTable(UserApiKeyModel.TableName)
             .HasKey(e => e.Id);
            b.HasIndex(e => e.Token).IsUnique(true);
            b.HasIndex(e => e.UserId).IsUnique(false);
            b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
            b.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .IsRequired(false);
        });
        builder.Entity<S3FileInformationModel>(
            b =>
            {
                b.ToTable(S3FileInformationModel.TableName);
                b.HasKey(e => e.Id);
                
                b.HasMany(e => e.Chunks)
                    .WithOne(e => e.S3FileInformation)
                    .HasForeignKey(e => e.FileId)
                    .IsRequired(true);
                b.HasOne(e => e.File)
                    .WithOne()
                    .HasForeignKey<S3FileInformationModel>(e => e.Id);
            });
        builder.Entity<ChunkUploadSessionModel>(
            b =>
            {
                b.ToTable(ChunkUploadSessionModel.TableName);
                b.HasKey(e => e.Id);
                
                b.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired(false);
                b.HasOne(e => e.File)
                    .WithMany()
                    .HasForeignKey(e => e.FileId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(true);
            });
        builder.Entity<FilePreviewModel>()
            .ToTable(FilePreviewModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<S3FileChunkModel>()
            .ToTable(S3FileChunkModel.TableName)
            .HasKey(e => e.Id);

        builder.Entity<TrustedProxyModel>(b =>
        {
            b.ToTable(TrustedProxyModel.TableName);
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.Address);
        });
        builder.Entity<TrustedProxyHeaderModel>(b =>
        {
            b.ToTable(TrustedProxyHeaderModel.TableName);
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.HeaderName);
        });
        builder.Entity<TrustedProxyHeaderMappingModel>(b =>
        {
            b.ToTable(TrustedProxyHeaderMappingModel.TableName);
            b.HasKey(e => e.Id);

            b.HasOne(e => e.TrustedProxy)
             .WithMany(e => e.HeaderMappings)
             .HasForeignKey(e => e.TrustedProxyId)
             .IsRequired(false);
             
            b.HasOne(e => e.TrustedProxyHeader)
             .WithMany(e => e.HeaderMappings)
             .HasForeignKey(e => e.TrustedProxyHeaderId)
             .IsRequired(false);
        });

        builder.Entity<SystemMailboxMessageModel>(
            b =>
            {
                b.ToTable(SystemMailboxMessageModel.TableName);
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.IsDeleted);
            });
    }
}