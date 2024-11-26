using kate.FileShare.Data.Models;
using kate.FileShare.Data.Models.Audit;
using kate.FileShare.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace kate.FileShare.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel>
{
    private readonly DbContextOptions<ApplicationDbContext> _ops;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { _ops = options; }

    public ApplicationDbContext CreateSession()
    {
        return new(_ops);
    }
    
    public DbSet<UserLimitModel> UserLimits { get; set; }
    public DbSet<PreferencesModel> Preferences { get; set; }
    public DbSet<FileModel> Files { get; set; }
    public DbSet<FilePreviewModel> FilePreviews { get; set; }
    public DbSet<S3FileInformationModel> S3FileInformations { get; set; }
    public DbSet<S3FileChunkModel> S3FileChunks { get; set; }
    public DbSet<ChunkUploadSessionModel> ChunkUploadSessions { get; set; } 

    public DbSet<AuditModel> Audit { get; set; }
    public DbSet<AuditEntryModel> AuditEntries { get; set; }

    public SystemSettingsParams GetSystemSettings()
    {
        var instance = new SystemSettingsParams();
        instance.Get(this);
        return instance;
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
        builder.Entity<PreferencesModel>()
            .ToTable(PreferencesModel.TableName)
            .HasKey(b => b.Key);

        builder.Entity<UserModel>().HasOne(e => e.Limit)
            .WithOne(e => e.User)
            .HasForeignKey<UserLimitModel>(e => e.UserId)
            .IsRequired(false);

        builder.Entity<FileModel>()
            .ToTable(FileModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<FileModel>().HasIndex(e => e.CreatedByUserId).IsUnique(false);
        builder.Entity<FilePreviewModel>()
            .ToTable(FilePreviewModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<S3FileInformationModel>()
            .ToTable(S3FileInformationModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<S3FileChunkModel>()
            .ToTable(S3FileChunkModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<ChunkUploadSessionModel>()
            .ToTable(ChunkUploadSessionModel.TableName)
            .HasKey(e => e.Id);

        builder.Entity<FileModel>()
            .HasOne(e => e.CreatedByUser)
            .WithOne()
            .HasForeignKey<FileModel>(e => e.CreatedByUserId)
            .IsRequired(false);
        builder.Entity<FileModel>()
            .HasOne(e => e.Preview)
            .WithOne(e => e.File)
            .HasForeignKey<FilePreviewModel>(e => e.Id)
            .IsRequired(false);

        builder.Entity<FileModel>()
            .HasOne(e => e.S3FileInformation)
            .WithOne(e => e.File)
            .HasForeignKey<S3FileInformationModel>(e => e.Id)
            .IsRequired(false);

        builder.Entity<S3FileInformationModel>()
            .HasMany(e => e.Chunks)
            .WithOne(e => e.S3FileInformation)
            .HasForeignKey(e => e.FileId)
            .IsRequired(true);
        builder.Entity<S3FileInformationModel>()
            .HasOne(e => e.File)
            .WithOne()
            .HasForeignKey<S3FileInformationModel>(e => e.Id);
        
        builder.Entity<ChunkUploadSessionModel>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false);
        builder.Entity<ChunkUploadSessionModel>()
            .HasOne(e => e.File)
            .WithMany()
            .HasForeignKey(e => e.FileId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(true);
    }
}