namespace CoreGrid.Api.Domain;

// Records state-changing operations for auditing.
public class AuditLogEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public required string Operation { get; set; } // Create | Update | Delete

    // JSON array of { field, before, after } for changed properties only.
    public string? Changes { get; set; }

    public Guid CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
