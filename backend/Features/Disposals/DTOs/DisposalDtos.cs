using System;
using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Disposals.DTOs;

public class CondemnAssetRequest
{
    [MaxLength(1000)]
    public string? Reason { get; set; }

    [MaxLength(2000)]
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
    [Required]
    public Guid? AssetId { get; set; }

    [Required]
    public DisposalMethod? DisposalMethod { get; set; }

    // P2 (disposal precondition) checks that a valuation amount is
    // recorded — an absent value defaulting to 0 would silently pass that
    // check instead of failing model validation.
    [Required]
    [Range(0, 1_000_000_000_000)]
    public decimal? EstimatedResidualValue { get; set; }

    public DateOnly? ValuationDate { get; set; }
    public string? Notes { get; set; }
}

public class RequestDisposalRevisionRequest
{
    [Required, MinLength(1), MaxLength(2000)]
    public required string Comments { get; set; }
}

// SRS §9.4: "Reject with a reason."
public class RejectDisposalRequest
{
    [Required, MinLength(1), MaxLength(2000)]
    public required string Reason { get; set; }
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

public class DisposalQueryParameters : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches this list's previous, only ordering.
    public DisposalQueryParameters()
    {
        SortDirection = "desc";
    }

    public DisposalStatus? Status { get; set; }
    public DisposalMethod? Method { get; set; }
}
