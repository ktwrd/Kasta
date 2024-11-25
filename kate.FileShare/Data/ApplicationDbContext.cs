﻿using kate.FileShare.Data.Models;
using kate.FileShare.Data.Models.Audit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace kate.FileShare.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }
    
    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserLimitModel> UserLimits { get; set; }
    public DbSet<PreferencesModel> Preferences { get; set; }
    public DbSet<FileModel> Files { get; set; }
    public DbSet<S3FileInformationModel> S3FileInformations { get; set; }
    public DbSet<S3FileChunkModel> S3FileChunks { get; set; }
    public DbSet<ChunkUploadSessionModel> ChunkUploadSessions { get; set; } 

    public DbSet<AuditModel> Audit { get; set; }
    public DbSet<AuditEntryModel> AuditEntries { get; set; }

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
        builder.Entity<S3FileInformationModel>()
            .ToTable(S3FileInformationModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<S3FileChunkModel>()
            .ToTable(S3FileChunkModel.TableName)
            .HasKey(e => e.Id);
        builder.Entity<ChunkUploadSessionModel>()
            .ToTable(ChunkUploadSessionModel.TableName)
            .HasKey(e => e.Id);

        // builder.Entity<FileModel>()
        //     .HasOne(e => e.CreatedByUser)
        //     .WithOne()
        //     .HasForeignKey<UserModel>(e => e.Id)
        //     .IsRequired(false);

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
        
        // builder.Entity<ChunkUploadSessionModel>()
        //     .HasOne(e => e.User)
        //     .WithOne()
        //     .HasForeignKey<UserModel>(e => e.Id)
        //     .IsRequired(false);
        // builder.Entity<ChunkUploadSessionModel>()
        //     .HasOne(e => e.File)
        //     .WithOne()
        //     .HasForeignKey<FileModel>(e => e.Id)
        //     .IsRequired(true);
    }
}