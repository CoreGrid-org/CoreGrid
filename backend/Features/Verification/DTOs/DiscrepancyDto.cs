using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Verification.DTOs;

public class DiscrepancyDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid VerificationTaskId { get; set; }
    public Guid AssetId { get; set; }
    public required string AssetCode { get; set; }

    public DiscrepancyType Type { get; set; }
    public bool IsAutomatic { get; set; }
    public Guid? RaisedByUserId { get; set; }
    public string? RaisedByEmail { get; set; }
    public required string Description { get; set; }
    public string? PhotoUrl { get; set; }

    public DiscrepancyStatus Status { get; set; }
    public string? ResolutionType { get; set; }
    public string? ResolutionExplanation { get; set; }
    public string? CorrectiveAction { get; set; }
    public bool RegisterCorrected { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class RaiseDiscrepancyRequest
{
    public DiscrepancyType Type { get; set; } = DiscrepancyType.Other;

    [Required, MaxLength(2000)]
    public required string Description { get; set; }

    public string? PhotoUrl { get; set; }
}

public class ResolveDiscrepancyRequest
{
    [Required, MaxLength(50)]
    public required string ResolutionType { get; set; }

    [Required, MaxLength(2000)]
    public required string ResolutionExplanation { get; set; }

    [MaxLength(2000)]
    public string? CorrectiveAction { get; set; }

    // Only supported for ConditionMismatch and LocationMismatch — those are
    // the only two discrepancy types with a single, unambiguous register
    // field to correct (Asset.Condition / Asset.LocationId), taken from the
    // officer's assertion already recorded on the originating task.
    public bool ApplyCorrection { get; set; }
}

public class DiscrepancyQueryParameters : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches this list's previous, only ordering.
    public DiscrepancyQueryParameters()
    {
        SortDirection = "desc";
    }

    public Guid? CampaignId { get; set; }
    public bool OnlyOpen { get; set; }
}
