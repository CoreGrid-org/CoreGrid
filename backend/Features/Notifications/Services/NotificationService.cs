using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Notifications.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreGrid.Api.Features.Notifications.Services;

public class NotificationService(CoreGridDbContext db, ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(
        Guid organizationId,
        Guid recipientUserId,
        string type,
        string title,
        string message,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        try
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RecipientUserId = recipientUserId,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // AC4: a notification failure must never roll back or fail the
            // business operation that triggered it.
            logger.LogWarning(ex,
                "Failed to create notification (type={Type}, recipient={RecipientUserId}) — isolated per AC4, not rethrown.",
                type, recipientUserId);
        }
    }

    public async Task<List<NotificationDto>> GetForUserAsync(
        Guid organizationId, Guid userId, bool onlyUnread, CancellationToken cancellationToken)
    {
        var query = db.Notifications
            .AsNoTracking()
            .Where(n => n.OrganizationId == organizationId && n.RecipientUserId == userId);

        if (onlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(
            n => n.OrganizationId == organizationId && n.RecipientUserId == userId && !n.IsRead,
            cancellationToken);

    public async Task<bool> MarkAsReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(
            n => n.Id == id && n.OrganizationId == organizationId && n.RecipientUserId == userId,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllAsReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var unread = await db.Notifications
            .Where(n => n.OrganizationId == organizationId && n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
