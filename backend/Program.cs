using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CoreGridDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CoreGrid")));

builder.Services.AddHttpClient<IIdentityDirectory, ThunderIdIdentityDirectory>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["ThunderID:Issuer"]
        ?? throw new InvalidOperationException("Missing required configuration 'ThunderID:Issuer'."));
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Same self-signed-certificate relaxation as the JWT bearer
        // backchannel below — ThunderID's local quick-start container.
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// Validates access tokens issued by ThunderID (SRS §4.5). Only the issuer
// and RS256 signature (via JWKS, auto-discovered from the issuer's
// /.well-known/openid-configuration) are checked — no audience validation.
// This mirrors OpenSchool's confirmed-working ThunderID integration, whose
// backend never registers a separate protected-resource audience either;
// see doc/setup/ThunderID.md's note on ThunderID__Audience for the same
// finding. The `roles` claim (a JSON array) is what CoreGridRole-based
// [Authorize(Roles = ...)] policies read, via RoleClaimType below.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["ThunderID:Issuer"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            RoleClaimType = "roles",
        };

        if (builder.Environment.IsDevelopment())
        {
            // ThunderID's local quick-start container serves a self-signed
            // certificate — relax TLS verification only for the backchannel
            // metadata/JWKS fetch, and only in Development.
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
        }
    });

builder.Services.AddAuthorization();

// SEC-ID-08: CORS shall permit only the configured origins of the deployed
// React application.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
