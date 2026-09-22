using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Transfers.DTOs;

public class InitiateTransferRequest
{
    [Required]
    public Guid? AssetId { get; set; }

    [Required]
    public Guid? ToDepartmentId { get; set; }

    [Required]
    public Guid? ToLocationId { get; set; }
}

//  "reject a transfer request, recording a decision reason."
public class RejectTransferRequest
{
    [Required, MinLength(1), MaxLength(1000)]
    public required string Reason { get; set; }
}

public class TransferResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;

    public Guid FromDepartmentId { get; set; }
    public string? FromDepartmentName { get; set; }

    public Guid ToDepartmentId { get; set; }
    public string? ToDepartmentName { get; set; }

    public Guid FromLocationId { get; set; }
    public string? FromLocationName { get; set; }

    public Guid ToLocationId { get; set; }
    public string? ToLocationName { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByUserEmail { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserEmail { get; set; }

    public Guid? ConfirmedByUserId { get; set; }
    public string? ConfirmedByUserEmail { get; set; }

    public TransferStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }

    public string? RejectionReason { get; set; }
}

public class TransferQueryParameters : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches this list's previous, only ordering.
    public TransferQueryParameters()
    {
        SortDirection = "desc";
    }

    public TransferStatus? Status { get; set; }
    public Guid? DepartmentId { get; set; }
}
