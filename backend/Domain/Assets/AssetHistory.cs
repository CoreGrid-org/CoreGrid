using System;

namespace CoreGrid.Api.Domain;

public class AssetHistory
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public required string EventType { get; set; } // STATUS_CHANGE | FIELD_AMENDMENT | ...
    public required string Description { get; set; }
    public string? PreviousValue { get; set; } // jsonb
    public string? NewValue { get; set; } // jsonb

    public DateTimeOffset CreatedAt { get; set; }
}
