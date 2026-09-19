using CoreGrid.Api.Features.Disposals.Services;

namespace CoreGrid.Api.Features.Disposals;

public static class DisposalsModule
{
    public static IServiceCollection AddDisposalsFeature(this IServiceCollection services)
    {
        services.AddScoped<IDisposalPreconditionService, DisposalPreconditionService>();
        services.AddScoped<IDisposalService, DisposalService>();

        return services;
    }
}
