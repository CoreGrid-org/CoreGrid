using CoreGrid.Api.Features.OrgConfig.Services;

namespace CoreGrid.Api.Features.OrgConfig;

public static class OrgConfigModule
{
    public static IServiceCollection AddOrgConfigFeature(this IServiceCollection services)
    {
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IOrganizationPolicyService, OrganizationPolicyService>();

        return services;
    }
}
