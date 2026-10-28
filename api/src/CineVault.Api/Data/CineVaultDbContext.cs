using Microsoft.EntityFrameworkCore;

namespace CineVault.Api.Data;

public class CineVaultDbContext(DbContextOptions<CineVaultDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<CastMember> Cast => Set<CastMember>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Movie>(e =>
        {
            e.Property(m => m.Title).HasMaxLength(300).IsRequired();
            e.Property(m => m.Overview).HasMaxLength(2000);
            e.HasIndex(m => m.Title);
            e.HasIndex(m => m.ReleaseDate);
            e.HasMany(m => m.Cast).WithOne().HasForeignKey(c => c.MovieId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(m => m.Ratings).WithOne().HasForeignKey(r => r.MovieId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CastMember>(e =>
        {
            e.ToTable("CastMembers");
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(c => new { c.MovieId, c.Position });
        });

        b.Entity<Rating>(e =>
        {
            e.HasKey(r => new { r.MovieId, r.UserId });
            e.ToTable(t => t.HasCheckConstraint("CK_Ratings_Stars", "Stars BETWEEN 1 AND 5"));
            e.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });
    }
}
