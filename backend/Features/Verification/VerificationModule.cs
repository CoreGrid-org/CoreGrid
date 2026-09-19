using CoreGrid.Api.Features.Verification.Services;

namespace CoreGrid.Api.Features.Verification;

public static class VerificationModule
{
    public static IServiceCollection AddVerificationFeature(this IServiceCollection services)
    {
        services.AddScoped<IVerificationCampaignService, VerificationCampaignService>();
        services.AddScoped<IVerificationTaskService, VerificationTaskService>();
        services.AddScoped<IDiscrepancyService, DiscrepancyService>();
        services.AddScoped<ICampaignReportService, CampaignReportService>();
        services.AddScoped<IAuditReportService, AuditReportService>();

        return services;
    }
}
