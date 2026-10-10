using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Notifications.DTOs;
using CoreGrid.Api.Features.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Tests.Features.Notifications;

// FR-080 in-app notifications: a user only ever sees, counts and marks
// their own notifications, and a failed write never surfaces to the
// business operation that triggered it (AC4).
[Trait("Component", "B")]
public class NotificationServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly CoreGridDbContext _db;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        _service = new NotificationService(_db, NullLogger<NotificationService>.Instance);
    }

    private Notification Add(Guid? recipient = null, Guid? orgId = null, bool isRead = false, DateTimeOffset? createdAt = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(), OrganizationId = orgId ?? _orgId, RecipientUserId = recipient ?? _userId,
            Type = NotificationTypes.MaintenanceAssigned, Title = "Assigned", Message = "You have work",
            IsRead = isRead, CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
        _db.Notifications.Add(notification);
        return notification;
    }

    [Fact]
    public async Task NotifyAsync_StoresAnUnreadNotification_ForTheRecipient()
    {
        var recordId = Guid.NewGuid();

        await _service.NotifyAsync(_orgId, _userId, NotificationTypes.MaintenanceAssigned,
            "Maintenance assigned to you", "Pump AST-1", "MaintenanceRecord", recordId, CancellationToken.None);

        var stored = await _db.Notifications.SingleAsync();
        Assert.Equal(_orgId, stored.OrganizationId);
        Assert.Equal(_userId, stored.RecipientUserId);
        Assert.Equal(NotificationTypes.MaintenanceAssigned, stored.Type);
        Assert.Equal("Maintenance assigned to you", stored.Title);
        Assert.Equal("MaintenanceRecord", stored.RelatedEntityType);
        Assert.Equal(recordId, stored.RelatedEntityId);
        Assert.False(stored.IsRead);
    }

    // AC4: a notification failure is logged and swallowed, never rethrown.
    [Fact]
    public async Task NotifyAsync_WhenTheWriteFails_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var brokenDb = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        await brokenDb.DisposeAsync();
        var service = new NotificationService(brokenDb, NullLogger<NotificationService>.Instance);

        var exception = await Record.ExceptionAsync(() => service.NotifyAsync(
            _orgId, _userId, NotificationTypes.MaintenanceCancelled, "Cancelled", "Gone", null, null, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyTheUsersOwnNotifications_NewestFirst()
    {
        var older = Add(createdAt: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = Add(createdAt: DateTimeOffset.UtcNow);
        Add(recipient: Guid.NewGuid());
        Add(orgId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync(_orgId, _userId, new NotificationQueryParameters(), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Items.Select(n => n.Id));
    }

    [Fact]
    public async Task GetForUserAsync_OnlyUnread_LeavesOutReadNotifications()
    {
        var unread = Add(isRead: false);
        Add(isRead: true);
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync(_orgId, _userId, new NotificationQueryParameters { OnlyUnread = true }, CancellationToken.None);

        Assert.Equal(unread.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyTheUsersUnreadNotifications()
    {
        Add();
        Add();
        Add(isRead: true);
        Add(recipient: Guid.NewGuid());
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _service.GetUnreadCountAsync(_orgId, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksTheUsersOwnNotification()
    {
        var notification = Add();
        await _db.SaveChangesAsync();

        Assert.True(await _service.MarkAsReadAsync(_orgId, _userId, notification.Id, CancellationToken.None));
        Assert.True((await _db.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_OnSomeoneElsesNotification_ReturnsFalse_AndLeavesItUnread()
    {
        var notification = Add(recipient: Guid.NewGuid());
        await _db.SaveChangesAsync();

        Assert.False(await _service.MarkAsReadAsync(_orgId, _userId, notification.Id, CancellationToken.None));
        Assert.False((await _db.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_UnknownId_ReturnsFalse()
    {
        Assert.False(await _service.MarkAsReadAsync(_orgId, _userId, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryOneOfTheUsersNotifications_AndNoOneElses()
    {
        Add();
        Add();
        var someoneElses = Add(recipient: Guid.NewGuid());
        await _db.SaveChangesAsync();

        await _service.MarkAllAsReadAsync(_orgId, _userId, CancellationToken.None);

        Assert.Equal(0, await _service.GetUnreadCountAsync(_orgId, _userId, CancellationToken.None));
        Assert.False((await _db.Notifications.SingleAsync(n => n.Id == someoneElses.Id)).IsRead);
    }
}
