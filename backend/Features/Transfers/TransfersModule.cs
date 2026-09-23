using CoreGrid.Api.Features.Transfers.Services;

namespace CoreGrid.Api.Features.Transfers;

public static class TransfersModule
{
    public static IServiceCollection AddTransfersFeature(this IServiceCollection services)
    {
        services.AddScoped<ITransferService, TransferService>();

        return services;
    }
}
