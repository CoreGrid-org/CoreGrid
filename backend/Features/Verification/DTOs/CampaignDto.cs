using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Verification.DTOs;

public class CampaignDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public Guid? ScopeDepartmentId { get; set; }
    public string? ScopeDepartmentName { get; set; }
    public Guid? ScopeLocationId { get; set; }
    public string? ScopeLocationName { get; set; }
    public Guid? ScopeAssetCategoryId { get; set; }
    public string? ScopeAssetCategoryName { get; set; }
    public Guid? ScopeAssetTypeId { get; set; }
    public string? ScopeAssetTypeName { get; set; }

    public CampaignStatus Status { get; set; }

    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OpenDiscrepancyCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateCampaignRequest
{
    public required string Name { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public Guid? ScopeDepartmentId { get; set; }
    public Guid? ScopeLocationId { get; set; }
    public Guid? ScopeAssetCategoryId { get; set; }
    public Guid? ScopeAssetTypeId { get; set; }
}
