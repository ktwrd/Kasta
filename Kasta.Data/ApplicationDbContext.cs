using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel>, IDataProtectionKeyContext
{
    private readonly DbContextOptions<ApplicationDbContext> _ops;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { _ops = options; }

    public ApplicationDbContext CreateSession()
    {
        return new(_ops);
    }
    #region IDataProtectionKeyContext
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    #endregion
    public DbSet<UserApiKeyModel> UserApiKeys { get; set; }
    public DbSet<UserSettingModel> UserSettings { get; set; }
    public DbSet<UserLimitModel> UserLimits { get; set; }
    public DbSet<PreferencesModel> Preferences { get; set; }
    public DbSet<FileModel> Files { get; set; }
    public DbSet<FilePreviewModel> FilePreviews { get; set; }
    public DbSet<S3FileInformationModel> S3FileInformations { get; set; }
    public DbSet<S3FileChunkModel> S3FileChunks { get; set; }
    public DbSet<ChunkUploadSessionModel> ChunkUploadSessions { get; set; } 

    public DbSet<AuditModel> Audit { get; set; }
    public DbSet<AuditEntryModel> AuditEntries { get; set; }

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

    public IQueryable<FileModel> SearchFiles(string? query, string? userId = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Files.Include(e => e.Preview);
            }
            else
            {
                return Files.Where(e => e.CreatedByUserId == userId).Include(e => e.Preview);
            }
        }
        else
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Files.Where(e => e.SearchVector.Matches(query)).Include(e => e.Preview);
            }
            else
            {
                return Files.Where(e => e.SearchVector.Matches(query) && e.CreatedByUserId == userId).Include(e => e.Preview);
            }
        }
    }

    public async Task<FileModel?> GetFileAsync(string id)
    {
        var target = await Files.Where(e => e.Id == id).Include(e => e.Preview).FirstOrDefaultAsync();
        if (target == null)
        {
            target = await Files.Where(e => e.ShortUrl == id).Include(e => e.Preview).FirstOrDefaultAsync();
        }
        return target;
    }
    public FileModel? GetFile(string id)
    {
        var target = Files.Where(e => e.Id == id).Include(e => e.Preview).FirstOrDefault();
        if (target == null)
        {
            target = Files.Where(e => e.ShortUrl == id).Include(e => e.Preview).FirstOrDefault();
        }
        return target;
    }
    public UserSettingModel GetUserSettings(UserModel user)
    {
        var r = UserSettings.Where(e => e.Id == user.Id).FirstOrDefault();
        if (r == null)
        {
            using (var ctx = CreateSession())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
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
            }
        }
        return r;
    }
    public async Task<UserSettingModel> GetUserSettingsAsync(UserModel user)
    {
        var r = await UserSettings.Where(e => e.Id == user.Id).FirstOrDefaultAsync();
        if (r == null)
        {
            using (var ctx = CreateSession())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
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
            }
        }
        return r;
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
                    .IsRequired(false);
                b.HasOne(e => e.Settings)
                    .WithOne(e => e.User)
                    .HasForeignKey<UserSettingModel>(e => e.Id)
                    .IsRequired(false);
                b.HasMany(e => e.ApiKeys)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .IsRequired(true);
            });

        builder.Entity<FileModel>(
            b =>
            {
                b.ToTable(FileModel.TableName);
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
                b.HasIndex(e => e.Filename).IsUnique(false);

                b.HasGeneratedTsVectorColumn(
                        p => p.SearchVector, "english", p => new
                        {
                            p.Filename
                        })
                    .HasIndex(p => p.SearchVector).HasMethod("GIN");

                b.HasOne(e => e.CreatedByUser)
                    .WithOne()
                    .HasForeignKey<FileModel>(e => e.CreatedByUserId)
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
        builder.Entity<UserApiKeyModel>(b =>
        {
            b.ToTable(UserApiKeyModel.TableName)
             .HasKey(e => e.Id);
            b.HasIndex(e => e.Token).IsUnique(true);
            b.HasIndex(e => e.UserId).IsUnique(false);
            b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
            b.HasOne(e => e.CreatedByUser)
                .WithOne()
                .HasForeignKey<UserApiKeyModel>(e => e.CreatedByUserId)
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
    }
}