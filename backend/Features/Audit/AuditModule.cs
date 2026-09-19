namespace CoreGrid.Api.Features.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditFeature(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
