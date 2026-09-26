using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Verification;

// The campaign report compares each completed verification with the register
// and classifies the outcome; the summary counts and value-at-risk build on it.
public class CampaignReportServiceTests
{
    private static CoreGridDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CoreGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new NullCurrentOrganizationProvider());

    [Fact]
    public async Task ClassifiesEachAssetAgainstTheRegister_AndSummarises()
    {
        await using var db = CreateDb();
        var org = Guid.NewGuid();
        var dept = new Department { Id = Guid.NewGuid(), OrganizationId = org, Code = "NHSL", Name = "National Hospital" };
        var ward = new Location { Id = Guid.NewGuid(), OrganizationId = org, DepartmentId = dept.Id, Name = "Ward 5", Type = "ward" };
        var etu = new Location { Id = Guid.NewGuid(), OrganizationId = org, DepartmentId = dept.Id, Name = "ETU", Type = "ward" };
        var category = new AssetCategory { Id = Guid.NewGuid(), OrganizationId = org, Code = "MED", Name = "Medical" };
        var type = new AssetType { Id = Guid.NewGuid(), OrganizationId = org, AssetCategoryId = category.Id, Code = "INF", Name = "Infusion Pump", UsefulLifeYears = 7 };
        var verifier = new User { Id = Guid.NewGuid(), OrganizationId = org, ExternalSubjectId = "s", Email = "v@x.lk", GivenName = "Dilani", FamilyName = "R", Role = CoreGridRole.Staff, IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
        var campaign = new VerificationCampaign { Id = Guid.NewGuid(), OrganizationId = org, Name = "Q3", PeriodStart = new DateOnly(2026, 7, 1), PeriodEnd = new DateOnly(2026, 9, 15), Status = CampaignStatus.Active, CreatedByUserId = verifier.Id, CreatedAt = DateTimeOffset.UtcNow };
        db.Add(new Organization { Id = org, Name = "Test Organisation" });
        db.AddRange(dept, ward, etu, category, type, verifier, campaign);

        Asset NewAsset(string code, decimal cost) => new()
        {
            Id = Guid.NewGuid(), OrganizationId = org, AssetTypeId = type.Id, DepartmentId = dept.Id, LocationId = ward.Id,
            AssetCode = code, Name = code, Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = code, AcquisitionCost = cost,
        };
        var found = NewAsset("A-1", 100);
        var missing = NewAsset("A-2", 250);
        var moved = NewAsset("A-3", 100);
        var worse = NewAsset("A-4", 100);
        var pending = NewAsset("A-5", 100);
        db.AddRange(found, missing, moved, worse, pending);

        VerificationTask Task(Asset a, bool? present, Guid? loc, string? condition) => new()
        {
            Id = Guid.NewGuid(), OrganizationId = org, CampaignId = campaign.Id, AssetId = a.Id, AssignedToUserId = verifier.Id,
            DueDate = new DateOnly(2020, 1, 1), // in the past, so a pending task on an active campaign is overdue
            Status = present is null ? VerificationTaskStatus.Pending : VerificationTaskStatus.Completed,
            AssertedPresent = present, AssertedLocationId = loc, AssertedCondition = condition,
            CompletedByUserId = present is null ? null : verifier.Id, CompletedAt = present is null ? null : DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.AddRange(
            Task(found, true, ward.Id, "GOOD"),
            Task(missing, false, null, null),
            Task(moved, true, etu.Id, "GOOD"),
            Task(worse, true, ward.Id, "POOR"),
            Task(pending, null, null, null));
        await db.SaveChangesAsync();

        var report = (await new CampaignReportService(db).GetReportAsync(org, campaign.Id, CancellationToken.None))!;

        string OutcomeOf(string code) => report.Tasks.Single(t => t.AssetCode == code).Outcome;
        Assert.Equal(VerificationOutcomes.Verified, OutcomeOf("A-1"));
        Assert.Equal(VerificationOutcomes.NotFound, OutcomeOf("A-2"));
        Assert.Equal(VerificationOutcomes.LocationMismatch, OutcomeOf("A-3"));
        Assert.Equal(VerificationOutcomes.ConditionMismatch, OutcomeOf("A-4"));
        Assert.Equal(VerificationOutcomes.Pending, OutcomeOf("A-5"));

        Assert.Equal(5, report.AssetsInScope);
        Assert.Equal(4, report.Verified);
        Assert.Equal(80, report.CompletionPercent);
        Assert.Equal(1, report.OverdueTasks);
        Assert.Equal(1, report.FoundAsRecorded);
        Assert.Equal(1, report.NotFound);
        Assert.Equal(250, report.ValueNotFound);
        Assert.Equal(650, report.ValueInScope);

        var verifierRow = Assert.Single(report.ByVerifier);
        Assert.Equal("Dilani R", verifierRow.Name);
        Assert.Equal(3, verifierRow.IssuesFound);
    }

    [Fact]
    public void PdfAndCsvRenderForAnEmptyCampaign()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var service = new CampaignReportService(CreateDb());
        var report = new CampaignReportDto
        {
            CampaignName = "Empty", Scope = "Whole register", PeriodStart = new DateOnly(2026, 1, 1), PeriodEnd = new DateOnly(2026, 1, 31),
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        Assert.NotEmpty(service.BuildPdf(report));
        Assert.NotEmpty(service.BuildCsv(report));
    }
}
