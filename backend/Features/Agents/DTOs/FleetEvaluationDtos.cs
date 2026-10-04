namespace CoreGrid.Api.Features.Agents.DTOs;

// What an evaluation targets: an asset type's active fleet, optionally
// narrowed to one asset of that type. Resolved once by the orchestrator and
// handed to every node, so no node re-reads it.
public sealed record EvaluationScope(
    Guid OrganizationId,
    Guid AssetTypeId,
    string AssetTypeName,
    string CategoryName,
    Guid? AssetId,
    string? AssetCode)
{
    public bool IsSingleAsset => AssetId.HasValue;
    public string Label => IsSingleAsset ? $"{AssetCode} ({AssetTypeName})" : $"{AssetTypeName} fleet ({CategoryName})";
}

// Policy Compliance output across the scope: the action chosen for each asset
// (the highest-ranked candidate the rule engine permits) and its verdict.
public class FleetEvaluationDto
{
    public int AssetCount { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = [];
    public int PassCount { get; set; }
    public int DeferredCount { get; set; } // NEEDS_REVISION — e.g. an open maintenance record
    public int BlockedCount { get; set; }  // FAIL
    public List<FleetAssetResultDto> Assets { get; set; } = [];
}

public class FleetAssetResultDto
{
    public Guid AssetId { get; set; }
    public required string AssetCode { get; set; }
    public required string Condition { get; set; }
    public required string Action { get; set; }
    public required string Verdict { get; set; } // PASS | NEEDS_REVISION | FAIL
    public bool IsHighImpact { get; set; }
    public decimal? Ratio { get; set; }
    public decimal ProjectedCost { get; set; }
    public required string Reason { get; set; }
}
