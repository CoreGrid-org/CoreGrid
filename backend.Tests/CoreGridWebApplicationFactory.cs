using CoreGrid.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace backend.Tests.Features.Authorization;

// Runs the real ASP.NET Core pipeline in-process — real [Authorize] policies,
// real RoleEnrichmentMiddleware, real AgentToolsAuthMiddleware — against an
// InMemory CoreGridDbContext (same convention as the rest of this project),
// with only JWT *identity* verification swapped for TestAuthHandler.
public class CoreGridWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    public CoreGridDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: DatabaseName)
            .Options;
        return new CoreGridDbContext(options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AddDbContext merges multiple configuration calls for the same
            // TContext via IDbContextOptionsConfiguration<T> rather than
            // replacing them — removing only DbContextOptions<T> leaves the
            // original Npgsql configuration layered in alongside this one,
            // and EF refuses to start with two providers registered.
            services.RemoveAll<DbContextOptions<CoreGridDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<CoreGridDbContext>>();
            services.AddDbContext<CoreGridDbContext>(options => options.UseInMemoryDatabase(DatabaseName));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}
