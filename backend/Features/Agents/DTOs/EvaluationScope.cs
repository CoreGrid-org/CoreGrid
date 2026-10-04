using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.Agents.DTOs;

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

    public AssetSelection Selection => AssetSelection.Fleet(OrganizationId, AssetTypeId, AssetId);

    public static EvaluationScope Of(AgentWorkflow workflow) => new(
        workflow.OrganizationId,
        workflow.AssetTypeId,
        workflow.AssetType?.Name ?? "Asset type",
        workflow.AssetType?.AssetCategory?.Name ?? string.Empty,
        workflow.AssetId,
        workflow.Asset?.AssetCode);
}
