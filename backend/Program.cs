using System.Text.Json;
using System.Text.Json.Serialization;
using CoreGrid.Api.Data;
using CoreGrid.Api.Data.Auditing;
using CoreGrid.Api.Features.Audit;
using CoreGrid.Api.Features.Identity;
using CoreGrid.Api.Features.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CoreGrid.Api.Features.Assets;
using CoreGrid.Api.Features.OrgConfig;
using CoreGrid.Api.Features.Verification;
using CoreGrid.Api.Features.Maintenance;
using CoreGrid.Api.Features.Disposals;
using CoreGrid.Api.Features.Transfers;
using CoreGrid.Api.Features.AgentTools;
using CoreGrid.Api.Features.Agents;
using CoreGrid.Api.Features.Shared.Api;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.CurrentUser;
using CoreGrid.Api.Features.Shared.Health;
using CoreGrid.Api.Features.Shared.Http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;




AppContext.SetSwitch("System.Net.Security.UseNetworkFramework", true);

// FR-084/FR-085 (campaign report PDF export) — Community licence, free for
// this project's size; must be set once before any Document.GeneratePdf().
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        // §5.4: catches anything a controller's own try/catch doesn't —
        // today that's only what previously fell through to ASP.NET Core's
        // default (an unhandled 500); as feature controllers are migrated
        // off their per-action try/catch (Phase 3), more of them reach this
        // one mapping instead.
        options.Filters.Add<ApiExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        // CoreGridRole reads/writes as its member name ("Administrator", not
        // 3) — matches ThunderID's `roles` claim strings and the frontend's
        // own CoreGridRole string union (frontend/src/features/auth/lib/roles.ts).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// NFR-11: reshapes [ApiController]'s automatic ModelState-invalid 400 into
// the same ErrorEnvelope every other 4xx/5xx uses — a wire addition, not a
// wire break, since `message` stays present (§7).
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your ThunderID access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

// §4.2/B15: the one current-user lookup per request. Populated by
// RoleEnrichmentMiddleware; read by CoreGridControllerBase,
// AuditSaveChangesInterceptor and CurrentOrganizationProvider instead of
// each running its own separate `sub` lookup.
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());

builder.Services.AddDbContext<CoreGridDbContext>((serviceProvider, options) =>
    options.UseNpgsql(
            builder.Configuration.GetConnectionString("CoreGrid"),
            // NFR-24: a transient Npgsql failure (dropped connection,
            // brief unavailability) retries before failing the request,
            // instead of surfacing as an immediate 500.
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddAssetsFeature();
builder.Services.AddOrgConfigFeature();
builder.Services.AddVerificationFeature();
builder.Services.AddMaintenanceFeature();
builder.Services.AddAuditFeature();
builder.Services.AddNotificationsFeature();
builder.Services.AddSingleton<CoreGrid.Api.Features.Shared.Storage.IFileStorageService, CoreGrid.Api.Features.Shared.Storage.CloudflareR2StorageService>();
builder.Services.AddDisposalsFeature();
builder.Services.AddTransfersFeature();
builder.Services.AddAgentToolsFeature();
builder.Services.AddAgentsFeature();

builder.Services.AddIdentityFeature(builder.Configuration, builder.Environment);

// Validates access tokens issued by ThunderID (SRS §4.5). Only the issuer
// and RS256 signature (via JWKS, auto-discovered from the issuer's
// /.well-known/openid-configuration) are checked — no audience validation.
// This mirrors OpenSchool's confirmed-working ThunderID integration, whose
// backend never registers a separate protected-resource audience either;
// see doc/setup/ThunderID.md's note on ThunderID__Audience for the same
// finding. The `roles` claim is what CoreGridRole-based
// [Authorize(Roles = ...)] policies read, via RoleClaimType below — but its
// value at this point is only ThunderID's, so RoleEnrichmentMiddleware
// (registered below, after UseAuthentication) overwrites it from CoreGrid's
// own Users.Role before UseAuthorization ever evaluates a policy.
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

// §4.4 / NFR-10: named policies (Appendix B) plus a fail-closed fallback —
// an endpoint with no [Authorize]/[AllowAnonymous] at all now requires an
// authenticated caller instead of defaulting to anonymous access.
builder.Services.AddCoreGridAuthorization();

builder.Services.AddCoreGridHealthChecks(builder.Environment);
builder.Services.AddCoreGridRateLimiting();

// SEC-ID-08: CORS shall permit only the configured origins of the deployed
// React application.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // FR-084/FR-085: report export filenames are set via
        // Content-Disposition, which isn't CORS-safelisted by default — the
        // frontend needs this to read the server-chosen filename.
        .WithExposedHeaders("Content-Disposition"));
});

var app = builder.Build();

// §5.4: every response gets a correlation id, first, so every other
// middleware and filter below can attach it to whatever it produces.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // In dev the frontend talks to the plain-HTTP endpoint (VITE_API_URL,
    // Cors:AllowedOrigins are both http://localhost:5173/:5083). Redirecting
    // to https://localhost:7240 there just breaks fetch() on the untrusted
    // dev cert — "Can't reach CoreGrid / Failed to fetch".
    app.UseHttpsRedirection();

    // NFR-09: HSTS only outside Development — the untrusted local dev
    // cert would otherwise get the browser to remember an HTTPS-only
    // policy for localhost.
    app.UseHsts();
}

app.UseCors("Frontend");

app.UseAuthentication();

// §5.10 fix: runs uniformly on every route now — RoleEnrichmentMiddleware
// itself detects the agent service principal (ServicePrincipal.Is) and
// skips only the Users-row lookup/401 for it, so a human caller hitting
// /api/agent-tools/* still gets its `roles` claim rehydrated like anywhere
// else. The old path-based UseWhen exclusion skipped rehydration outright
// for *any* caller on those routes, which is what broke
// HumanUser_CanAlsoReachAgentToolsAsThemselves (403 instead of 200) once
// AgentToolsController moved onto the CanReadAssets policy.
app.UseMiddleware<RoleEnrichmentMiddleware>();

// SEC-ID-09: observes the 401/403 UseAuthorization below decides.
app.UseMiddleware<AuthorizationOutcomeLoggingMiddleware>();

app.UseAuthorization();

// NFR-16/AI-27: policies are defined (Features/Shared/Http/RateLimiting.cs)
// but not yet attached to any route via [EnableRateLimiting] — that lands
// per-controller in Phase 3 alongside each one's other policy migration.
app.UseRateLimiter();

// NFR-20: anonymous by design — a caller checking liveness/readiness has
// no token to present, and the check itself must never require the
// dependency it's reporting on to already be healthy.
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckExtensions.WriteResponse }).AllowAnonymous();

app.MapControllers();

app.Run();

// Exposes the top-level Program class to WebApplicationFactory<Program> for
// integration testing (CoreGrid.Api.Tests) — otherwise it stays internal.
public partial class Program { }
