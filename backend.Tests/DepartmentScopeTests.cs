using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using CoreGrid.Api.Features.Shared.Scoping;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Shared;

// Phase 6 (§8) / FR-086 / Appendix B: "Staff are restricted to their own
// department by a service-layer filter" — applied to the Assets,
// Maintenance, Transfers and Disposals list/detail endpoints (B14, §4.5).
// DepartmentScope.For/ApplyScope is the one shared implementation all four
// services call through, so it's pinned here once; AssetService.GetAssetsAsync
// then proves a real call site actually wires it in, rather than just
// existing unused.
public class DepartmentScopeTests
{
    [Fact]
    public void For_Administrator_IsUnrestricted()
    {
        var user = new User { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), ExternalSubjectId = "s", Email = "a@b.com", GivenName = "A", FamilyName = "B", Role = CoreGridRole.Administrator, DepartmentId = Guid.NewGuid() };

        var scope = DepartmentScope.For(user);

        Assert.False(scope.IsRestricted);
    }

    [Fact]
    public void For_Auditor_IsUnrestricted()
    {
        var user = new User { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), ExternalSubjectId = "s", Email = "a@b.com", GivenName = "A", FamilyName = "B", Role = CoreGridRole.Auditor, DepartmentId = Guid.NewGuid() };

        var scope = DepartmentScope.For(user);

        Assert.False(scope.IsRestricted);
    }

    [Theory]
    [InlineData(CoreGridRole.Staff)]
    [InlineData(CoreGridRole.InventoryOfficer)]
    public void For_StaffOrOfficer_IsRestrictedToTheirOwnDepartment(CoreGridRole role)
    {
        var departmentId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), ExternalSubjectId = "s", Email = "a@b.com", GivenName = "A", FamilyName = "B", Role = role, DepartmentId = departmentId };

        var scope = DepartmentScope.For(user);

        Assert.True(scope.IsRestricted);
        Assert.Equal(departmentId, scope.DepartmentId);
    }

    [Fact]
    public void ApplyScope_Unrestricted_ReturnsEveryRow()
    {
        var rows = new[] { (Id: 1, DepartmentId: (Guid?)Guid.NewGuid()), (Id: 2, DepartmentId: (Guid?)Guid.NewGuid()) }.AsQueryable();

        var result = rows.ApplyScope(DepartmentScope.Unrestricted, r => r.DepartmentId).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyScope_RestrictedWithNoDepartmentAssigned_ReturnsNothing()
    {
        var rows = new[] { (Id: 1, DepartmentId: (Guid?)Guid.NewGuid()) }.AsQueryable();

        var result = rows.ApplyScope(DepartmentScope.Restricted(null), r => r.DepartmentId).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyScope_RestrictedWithADepartment_ExcludesEveryOtherDepartment()
    {
        var own = Guid.NewGuid();
        var other = Guid.NewGuid();
        var rows = new[] { (Id: 1, DepartmentId: (Guid?)own), (Id: 2, DepartmentId: (Guid?)other) }.AsQueryable();

        var result = rows.ApplyScope(DepartmentScope.Restricted(own), r => r.DepartmentId).ToList();

        var row = Assert.Single(result);
        Assert.Equal(1, row.Id);
    }

    [Fact]
    public async Task AssetService_GetAssetsAsync_StaffScope_OnlyReturnsTheirOwnDepartmentsAssets()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());

        var orgId = Guid.NewGuid();
        var ownDepartment = Guid.NewGuid();
        var otherDepartment = Guid.NewGuid();

        // Asset.AssetTypeId/DepartmentId/LocationId are non-nullable, so EF
        // treats those navigations as required and left-joins on them even
        // for a null-checked projection (AssetDto.DepartmentName etc.) —
        // against InMemory specifically, a required navigation with no
        // matching row silently drops the whole row rather than nulling
        // the field, so every FK here needs a real row to join to.
        var assetType = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgId, AssetCategoryId = Guid.NewGuid(), Code = "TY", Name = "Type", UsefulLifeYears = 5 };
        var location = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = ownDepartment, Name = "Store", Type = "store" };
        db.AssetTypes.Add(assetType);
        db.Locations.Add(location);
        db.Departments.AddRange(
            new Department { Id = ownDepartment, OrganizationId = orgId, Code = "OWN", Name = "Own Department" },
            new Department { Id = otherDepartment, OrganizationId = orgId, Code = "OTH", Name = "Other Department" });

        db.Assets.AddRange(
            new Asset { Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = assetType.Id, DepartmentId = ownDepartment, LocationId = location.Id, AssetCode = "AST-OWN", Name = "Own-department asset", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr-1" },
            new Asset { Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = assetType.Id, DepartmentId = otherDepartment, LocationId = location.Id, AssetCode = "AST-OTHER", Name = "Other-department asset", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr-2" });
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        var result = await service.GetAssetsAsync(orgId, DepartmentScope.Restricted(ownDepartment), new AssetQueryParameters(), CancellationToken.None);

        var asset = Assert.Single(result.Items);
        Assert.Equal("AST-OWN", asset.AssetCode);
    }
}
