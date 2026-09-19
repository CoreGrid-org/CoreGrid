using CoreGrid.Api.Features.Notifications.Services;

namespace CoreGrid.Api.Features.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
