using Kasta.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Data;

public class PostgresDbContext : KastaDbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
        AllowFileSearchVector = true;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<FileModel>()
            .HasGeneratedTsVectorColumn(
                    p => p.SearchVector, "english", p => new
                    {
                        p.Filename,
                        p.MimeType,
                        p.ShortUrl
                    })
                .HasIndex(p => p.SearchVector).HasMethod("GIN");
    }
}