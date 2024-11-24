using kate.FileShare.Data.Models;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserLimitModel>()
            .ToTable("UserLimits")
            .HasKey(e => e.UserId);
        builder.Entity<PreferencesModel>()
            .ToTable("Preferences")
            .HasKey(b => b.Key);

        builder.Entity<UserModel>().HasOne(e => e.Limit)
            .WithOne(e => e.User)
            .HasForeignKey<UserLimitModel>(e => e.UserId)
            .IsRequired(false);
    }
}