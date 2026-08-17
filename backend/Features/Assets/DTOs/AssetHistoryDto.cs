namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetHistoryDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
