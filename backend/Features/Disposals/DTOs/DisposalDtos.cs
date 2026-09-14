using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Disposals.DTOs;

public class CondemnAssetRequest
{
    public string? Reason { get; set; }
    public string? EvidenceUrl { get; set; }
}

public class CondemnAssetResponse
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset CondemnedAt { get; set; }
}

public class SubmitDisposalRequest
{
    public Guid AssetId { get; set; }
    public DisposalMethod DisposalMethod { get; set; }
    public decimal EstimatedResidualValue { get; set; }
    public DateOnly? ValuationDate { get; set; }
    public string? Notes { get; set; }
}

public class DisposalResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetCondition { get; set; } = string.Empty;
    public string AssetStatus { get; set; } = string.Empty;

    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByUserEmail { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserEmail { get; set; }

    public DisposalMethod DisposalMethod { get; set; }
    public decimal EstimatedResidualValue { get; set; }
    public DateOnly? ValuationDate { get; set; }

    public DisposalStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? DisposedAt { get; set; }

    public string? Notes { get; set; }

    // Evaluated dynamically on GET detail / response
    public DisposalPreconditionResult? PreconditionEvaluation { get; set; }
}

public class DisposalQueryParameters
{
    public DisposalStatus? Status { get; set; }
    public DisposalMethod? Method { get; set; }
}
