namespace CoreGrid.Api.Domain;

// FR-063/FR-064: an immutable record of every state-changing operation.
// Written only by AuditSaveChangesInterceptor — never created, updated or
// deleted through any controller — and the database itself revokes
// UPDATE/DELETE from the app role (see the AddAuditLog migration), the same
// pattern already used for AssetHistory.
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
