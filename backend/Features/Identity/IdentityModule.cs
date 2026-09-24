namespace CoreGrid.Api.Features.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityFeature(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddScoped<ICurrentOrganizationProvider, CurrentOrganizationProvider>();

        services.AddHttpClient<IIdentityDirectory, ThunderIdIdentityDirectory>((_, client) =>
        {
            client.BaseAddress = new Uri(configuration["ThunderID:Issuer"]
                ?? throw new InvalidOperationException("Missing required configuration 'ThunderID:Issuer'."));
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (environment.IsDevelopment())
            {
                // Allows the local development certificate.
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        return services;
    }
}
