using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class NotificationQueryParameters : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches this list's previous, only ordering.
    public NotificationQueryParameters()
    {
        SortDirection = "desc";
    }

    public bool OnlyUnread { get; set; }
}
