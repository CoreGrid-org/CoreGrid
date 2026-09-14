using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Notifications.DTOs;
using CoreGrid.Api.Features.Notifications.Services;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Notifications.Controllers;

// FR-080: every authenticated role can read/manage their own notifications
// — same "no role restriction needed" reasoning as MeController, since this
// is identity-scoped (the caller's own inbox), not a shared resource.
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService, CoreGridDbContext db) : CoreGridControllerBase(db)
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        [FromQuery] bool onlyUnread, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await notificationService.GetForUserAsync(currentUser.OrganizationId, currentUser.Id, onlyUnread, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var count = await notificationService.GetUnreadCountAsync(currentUser.OrganizationId, currentUser.Id, cancellationToken);
        return Ok(new UnreadCountDto { Count = count });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var found = await notificationService.MarkAsReadAsync(currentUser.OrganizationId, currentUser.Id, id, cancellationToken);
        if (!found) return NotFound(new { message = "Notification not found." });

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        await notificationService.MarkAllAsReadAsync(currentUser.OrganizationId, currentUser.Id, cancellationToken);
        return NoContent();
    }
}
