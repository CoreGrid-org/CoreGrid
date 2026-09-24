namespace CoreGrid.Api.Domain;

// Represents an in-app notification for a user.
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

    // Identifies the related entity for navigation.
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
