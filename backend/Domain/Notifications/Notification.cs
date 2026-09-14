namespace CoreGrid.Api.Domain;

// FR-080: in-app Notification Centre. Deliberately in-app only — no email
// (FR-077-079 is explicitly deferred to a later phase, see doc/PROGRESS.md) —
// so "dispatch" here just means writing this row; there is no external
// delivery step to fail independently of the database write itself.
public class Notification
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }

    public required string Type { get; set; } // MAINTENANCE_ASSIGNED, MAINTENANCE_COMPLETED, MAINTENANCE_CANCELLED, ...
    public required string Title { get; set; }
    public required string Message { get; set; }

    // Optional deep-link target (e.g. "MaintenanceRecord" + its Id) so the
    // frontend can route straight to the record a notification is about.
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
