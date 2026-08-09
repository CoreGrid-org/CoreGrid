using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Data;

public class CoreGridDbContext(DbContextOptions<CoreGridDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.ExternalSubjectId).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
