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

public class AuditLogQueryParameters
{
    public string? EntityType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Operation { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
