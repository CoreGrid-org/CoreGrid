using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Data;

public class CoreGridDbContext(DbContextOptions<CoreGridDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();
    public DbSet<DisposalRequest> DisposalRequests => Set<DisposalRequest>();

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

        modelBuilder.Entity<AssetTransfer>(entity =>
        {
            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            entity.HasOne(t => t.Organization)
                .WithMany()
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.InitiatedByUser)
                .WithMany()
                .HasForeignKey(t => t.InitiatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ApprovedByUser)
                .WithMany()
                .HasForeignKey(t => t.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(t => t.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DisposalRequest>(entity =>
        {
            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            entity.Property(d => d.EstimatedResidualValue)
                .HasPrecision(18, 2);

            entity.HasOne(d => d.Organization)
                .WithMany()
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.InitiatedByUser)
                .WithMany()
                .HasForeignKey(d => d.InitiatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ApprovedByUser)
                .WithMany()
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
