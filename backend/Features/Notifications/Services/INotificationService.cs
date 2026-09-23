using CoreGrid.Api.Features.Notifications.DTOs;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Notifications.Services;

public interface INotificationService
{
    // Deliberately fire-and-forget from the caller's point of view: never
    // throws. AC4 (notification failure isolation) — a failure here is
    // logged and swallowed, not surfaced to the business operation that
    // triggered it (FR-080's in-app row has no external dispatch step to
    // fail independently of the DB write, but this keeps the same isolation
    // contract the SRS describes regardless).
    Task NotifyAsync(
        Guid organizationId,
        Guid recipientUserId,
        string type,
        string title,
        string message,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken);

    Task<PagedResult<NotificationDto>> GetForUserAsync(Guid organizationId, Guid userId, NotificationQueryParameters query, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    Task<bool> MarkAsReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken);

    Task MarkAllAsReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
}
