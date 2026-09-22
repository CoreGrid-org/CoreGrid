using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Audit;

public class AuditLogEntryDto
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }

    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public required string Operation { get; set; }

    public string? Changes { get; set; }

    public Guid CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class AuditLogQueryParameters : PagedQuery
{
 // Configures the default audit log sorting and page size.
    public AuditLogQueryParameters()
    {
        PageSize = 50;
        SortDirection = "desc";
    }

    public string? EntityType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Operation { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
