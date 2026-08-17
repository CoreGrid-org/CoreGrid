using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Data;

public class CoreGridDbContext(DbContextOptions<CoreGridDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<OrganizationPolicy> OrganizationPolicies => Set<OrganizationPolicy>();
    public DbSet<AssetAttributeDefinition> AssetAttributeDefinitions => Set<AssetAttributeDefinition>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAttributeValue> AssetAttributeValues => Set<AssetAttributeValue>();
    public DbSet<AssetHistory> AssetHistoryEntries => Set<AssetHistory>();
    public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();
    public DbSet<DisposalRequest> DisposalRequests => Set<DisposalRequest>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<VerificationCampaign> VerificationCampaigns => Set<VerificationCampaign>();
    public DbSet<VerificationTask> VerificationTasks => Set<VerificationTask>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.ExternalSubjectId).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.OrganizationId);
            entity.HasIndex(u => u.DepartmentId);

            entity.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(d => d.OrganizationId);
            entity.HasIndex(d => new { d.OrganizationId, d.Code }).IsUnique();

            entity.Property(d => d.Code).HasMaxLength(20).IsRequired();
            entity.Property(d => d.Name).IsRequired();

            entity.HasOne(d => d.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(l => l.OrganizationId);
            entity.HasIndex(l => l.DepartmentId);

            entity.Property(l => l.Name).IsRequired();
            entity.Property(l => l.Type).HasMaxLength(30).IsRequired();

            entity.HasOne(l => l.Organization)
                .WithMany(o => o.Locations)
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Department)
                .WithMany(d => d.Locations)
                .HasForeignKey(l => l.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasIndex(ac => ac.OrganizationId);
            entity.HasIndex(ac => new { ac.OrganizationId, ac.Code }).IsUnique();

            entity.Property(ac => ac.Code).HasMaxLength(20).IsRequired();
            entity.Property(ac => ac.Name).IsRequired();

            entity.HasOne(ac => ac.Organization)
                .WithMany(o => o.AssetCategories)
                .HasForeignKey(ac => ac.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasIndex(at => at.OrganizationId);
            entity.HasIndex(at => at.AssetCategoryId);
            entity.HasIndex(at => new { at.OrganizationId, at.Code }).IsUnique();

            entity.Property(at => at.Code).HasMaxLength(20).IsRequired();
            entity.Property(at => at.Name).IsRequired();

            entity.HasOne(at => at.Organization)
                .WithMany(o => o.AssetTypes)
                .HasForeignKey(at => at.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(at => at.AssetCategory)
                .WithMany(ac => ac.AssetTypes)
                .HasForeignKey(at => at.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(tb => tb.HasCheckConstraint("CK_AssetTypes_UsefulLifeYears", "\"UsefulLifeYears\" > 0"));
        });

        modelBuilder.Entity<OrganizationPolicy>(entity =>
        {
            entity.HasIndex(op => op.OrganizationId);
            entity.HasIndex(op => new { op.OrganizationId, op.AssetTypeId }).IsUnique();

            entity.HasOne(op => op.Organization)
                .WithMany(o => o.OrganizationPolicies)
                .HasForeignKey(op => op.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(op => op.AssetType)
                .WithMany(at => at.OrganizationPolicies)
                .HasForeignKey(op => op.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(op => op.RepairToReplaceCostThreshold).HasPrecision(5, 2);
            entity.Property(op => op.MinimumServiceLifeYears).HasPrecision(5, 2);
            entity.Property(op => op.MaxAcceptableFailureFrequency).HasPrecision(5, 2);
            entity.Property(op => op.ConfidenceFloor).HasPrecision(5, 2);
            entity.Property(op => op.CostVarianceTolerancePercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<AssetAttributeDefinition>(entity =>
        {
            entity.HasIndex(aad => aad.AssetTypeId);
            entity.HasIndex(aad => new { aad.AssetTypeId, aad.Name }).IsUnique();

            entity.Property(aad => aad.Name).IsRequired();
            entity.Property(aad => aad.DataType).HasMaxLength(10).IsRequired();
            entity.Property(aad => aad.SelectOptions).HasColumnType("jsonb");

            entity.HasOne(aad => aad.AssetType)
                .WithMany(at => at.AssetAttributeDefinitions)
                .HasForeignKey(aad => aad.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_AssetAttributeDefinitions_DataType", "\"DataType\" IN ('TEXT','NUMBER','DATE','BOOLEAN','SELECT')");
                tb.HasCheckConstraint("CK_AssetAttributeDefinitions_SelectOptions", "\"DataType\" <> 'SELECT' OR \"SelectOptions\" IS NOT NULL");
            });
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasIndex(a => a.OrganizationId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.Condition);
            entity.HasIndex(a => a.DepartmentId);
            entity.HasIndex(a => a.AssetTypeId);
            entity.HasIndex(a => a.LocationId);
            entity.HasIndex(a => a.Name);
            entity.HasIndex(a => new { a.OrganizationId, a.AssetCode }).IsUnique();

            entity.Property(a => a.AssetCode).HasMaxLength(40).IsRequired();
            entity.Property(a => a.Name).IsRequired();
            entity.Property(a => a.Status).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Condition).HasMaxLength(15).IsRequired();
            entity.Property(a => a.QrPayload).IsRequired();

            entity.Property(a => a.AcquisitionCost).HasPrecision(18, 2);
            entity.Property(a => a.ResidualValue).HasPrecision(18, 2);
            entity.Property(a => a.CumulativeMaintenanceCost).HasPrecision(18, 2);

            entity.HasOne(a => a.Organization)
                .WithMany(o => o.Assets)
                .HasForeignKey(a => a.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AssetType)
                .WithMany(at => at.Assets)
                .HasForeignKey(a => a.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Department)
                .WithMany(d => d.Assets)
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Location)
                .WithMany(l => l.Assets)
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_Assets_Status", "\"Status\" IN ('ACTIVE','UNDER_MAINTENANCE','TRANSFER_REQUESTED','IN_TRANSIT','CONDEMNED','DISPOSAL_REQUESTED','DISPOSED')");
                tb.HasCheckConstraint("CK_Assets_Condition", "\"Condition\" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')");
                tb.HasCheckConstraint("CK_Assets_AcquisitionCost", "\"AcquisitionCost\" >= 0");
            });
        });

        modelBuilder.Entity<AssetAttributeValue>(entity =>
        {
            entity.HasIndex(aav => new { aav.AssetId, aav.AssetAttributeDefinitionId }).IsUnique();
            entity.HasIndex(aav => new { aav.AssetAttributeDefinitionId, aav.ValueText });
            entity.HasIndex(aav => new { aav.AssetAttributeDefinitionId, aav.ValueNumber });

            entity.Property(aav => aav.ValueNumber).HasPrecision(18, 4);

            entity.HasOne(aav => aav.Asset)
                .WithMany(a => a.AssetAttributeValues)
                .HasForeignKey(aav => aav.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(aav => aav.AssetAttributeDefinition)
                .WithMany(aad => aad.AssetAttributeValues)
                .HasForeignKey(aav => aav.AssetAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_AssetAttributeValues_ExactlyOneValue", "num_nonnulls(\"ValueText\", \"ValueNumber\", \"ValueDate\", \"ValueBoolean\") = 1");
            });
        });

        modelBuilder.Entity<AssetHistory>(entity =>
        {
            entity.ToTable("AssetHistory");
            entity.HasIndex(ah => ah.OrganizationId);
            entity.HasIndex(ah => ah.AssetId);
            entity.HasIndex(ah => ah.CreatedAt);

            entity.Property(ah => ah.EventType).HasMaxLength(30).IsRequired();
            entity.Property(ah => ah.Description).IsRequired();
            entity.Property(ah => ah.PreviousValue).HasColumnType("jsonb");
            entity.Property(ah => ah.NewValue).HasColumnType("jsonb");

            entity.HasOne(ah => ah.Organization)
                .WithMany(o => o.AssetHistoryEntries)
                .HasForeignKey(ah => ah.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ah => ah.Asset)
                .WithMany(a => a.AssetHistoryEntries)
                .HasForeignKey(ah => ah.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ah => ah.ActorUser)
                .WithMany(u => u.AssetHistoryEntries)
                .HasForeignKey(ah => ah.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_AssetHistory_EventType", "\"EventType\" IN ('STATUS_CHANGE','FIELD_AMENDMENT','VERIFICATION','MAINTENANCE','TRANSFER','DISPOSAL','AGENT_RECOMMENDATION')");
            });
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

            entity.HasOne(t => t.Asset)
                .WithMany()
                .HasForeignKey(t => t.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.FromDepartment)
                .WithMany()
                .HasForeignKey(t => t.FromDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToDepartment)
                .WithMany()
                .HasForeignKey(t => t.ToDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.FromLocation)
                .WithMany()
                .HasForeignKey(t => t.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToLocation)
                .WithMany()
                .HasForeignKey(t => t.ToLocationId)
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

            entity.HasOne(d => d.Asset)
                .WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasIndex(a => a.OrganizationId);
            entity.HasIndex(a => a.ActorUserId);
            entity.HasIndex(a => a.EntityType);
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.CorrelationId);

            entity.Property(a => a.EntityType).HasMaxLength(60).IsRequired();
            entity.Property(a => a.Operation).HasMaxLength(10).IsRequired();
            entity.Property(a => a.Changes).HasColumnType("jsonb");

            entity.HasOne(a => a.Organization)
                .WithMany()
                .HasForeignKey(a => a.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ActorUser)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_AuditLogEntries_Operation", "\"Operation\" IN ('Create','Update','Delete')");
            });
        });

        modelBuilder.Entity<VerificationCampaign>(entity =>
        {
            entity.HasIndex(c => c.OrganizationId);
            entity.HasIndex(c => c.Status);

            entity.Property(c => c.Name).IsRequired();

            entity.HasOne(c => c.Organization)
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ScopeDepartment)
                .WithMany()
                .HasForeignKey(c => c.ScopeDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ScopeLocation)
                .WithMany()
                .HasForeignKey(c => c.ScopeLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ScopeAssetCategory)
                .WithMany()
                .HasForeignKey(c => c.ScopeAssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ScopeAssetType)
                .WithMany()
                .HasForeignKey(c => c.ScopeAssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VerificationTask>(entity =>
        {
            entity.HasIndex(t => t.OrganizationId);
            entity.HasIndex(t => t.CampaignId);
            entity.HasIndex(t => t.AssetId);
            entity.HasIndex(t => t.AssignedToUserId);
            entity.HasIndex(t => t.Status);

            entity.HasOne(t => t.Organization)
                .WithMany()
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Campaign)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Asset)
                .WithMany()
                .HasForeignKey(t => t.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssertedLocation)
                .WithMany()
                .HasForeignKey(t => t.AssertedLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.CompletedByUser)
                .WithMany()
                .HasForeignKey(t => t.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Discrepancy>(entity =>
        {
            entity.HasIndex(d => d.OrganizationId);
            entity.HasIndex(d => d.CampaignId);
            entity.HasIndex(d => d.VerificationTaskId);
            entity.HasIndex(d => d.AssetId);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.Type);

            entity.Property(d => d.Description).IsRequired();

            entity.HasOne(d => d.Organization)
                .WithMany()
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Campaign)
                .WithMany()
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.VerificationTask)
                .WithMany(t => t.Discrepancies)
                .HasForeignKey(d => d.VerificationTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Asset)
                .WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RaisedByUser)
                .WithMany()
                .HasForeignKey(d => d.RaisedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ResolvedByUser)
                .WithMany()
                .HasForeignKey(d => d.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasIndex(m => m.OrganizationId);
            entity.HasIndex(m => m.AssetId);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.Priority);
            entity.HasIndex(m => m.Type);
            entity.HasIndex(m => m.AssigneeId);

            entity.Property(m => m.Description).IsRequired();
            entity.Property(m => m.ObservedCondition).HasMaxLength(15).IsRequired();
            entity.Property(m => m.ResultingCondition).HasMaxLength(15);
            entity.Property(m => m.PhotoUrl).HasMaxLength(500);
            entity.Property(m => m.CancellationReason).HasMaxLength(500);

            entity.Property(m => m.EstimatedCost).HasPrecision(18, 2);
            entity.Property(m => m.ActualCost).HasPrecision(18, 2);

            entity.Property(m => m.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(m => m.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(m => m.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(m => m.Organization)
                .WithMany()
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Asset)
                .WithMany(a => a.MaintenanceRecords)
                .HasForeignKey(m => m.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Assignee)
                .WithMany()
                .HasForeignKey(m => m.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_MaintenanceRecords_Status", "\"Status\" IN ('REQUESTED','APPROVED','IN_PROGRESS','COMPLETED','CANCELLED')");
                tb.HasCheckConstraint("CK_MaintenanceRecords_Priority", "\"Priority\" IN ('LOW','MEDIUM','HIGH','CRITICAL')");
                tb.HasCheckConstraint("CK_MaintenanceRecords_Type", "\"Type\" IN ('CORRECTIVE','PREVENTIVE')");
                tb.HasCheckConstraint("CK_MaintenanceRecords_ObservedCondition", "\"ObservedCondition\" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')");
                tb.HasCheckConstraint("CK_MaintenanceRecords_ResultingCondition", "\"ResultingCondition\" IS NULL OR \"ResultingCondition\" IN ('NEW','GOOD','FAIR','POOR','UNSERVICEABLE')");
            });
        });
    }
}
