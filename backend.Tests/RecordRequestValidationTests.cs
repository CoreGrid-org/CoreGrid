using System.Net;
using System.Net.Http.Json;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace backend.Tests.Features.Authorization;

// Positional-record request models must put their validation attributes on
// the constructor parameters, not `[property: ...]`. With the latter, MVC
// throws "Record type ... has validation metadata defined on property ...
// that will be ignored" on every request, which surfaced as a 500 "An
// unexpected error occurred" when creating a user. These go through the
// real pipeline so model validation actually runs.
public class RecordRequestValidationTests(CoreGridWebApplicationFactory factory) : IClassFixture<CoreGridWebApplicationFactory>, IAsyncLifetime
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly string _adminSubject = $"record-validation-admin-{Guid.NewGuid():N}";
    private Guid _staffUserId;

    private sealed class StubIdentityDirectory : IIdentityDirectory
    {
        public Task<string> ProvisionUserAsync(string email, string givenName, string familyName, string password, CoreGridRole role, CancellationToken cancellationToken)
            => Task.FromResult($"stub-{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        using var db = factory.CreateDbContext();
        db.Organizations.Add(new Organization { Id = _orgId, Name = "Record validation org" });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = _adminSubject, Email = $"admin-{Guid.NewGuid():N}@test.local",
            GivenName = "Admin", FamilyName = "User", Role = CoreGridRole.Administrator, IsActive = true, CreatedAt = DateTimeOffset.UtcNow,
        });
        _staffUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _staffUserId, OrganizationId = _orgId, ExternalSubjectId = $"staff-{Guid.NewGuid():N}", Email = $"staff-{Guid.NewGuid():N}@test.local",
            GivenName = "Staff", FamilyName = "User", Role = CoreGridRole.Staff, IsActive = true, CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AdminClient()
    {
        var client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityDirectory>();
            services.AddScoped<IIdentityDirectory, StubIdentityDirectory>();
        })).CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", _adminSubject);
        return client;
    }

    [Fact]
    public async Task CreateUser_ValidRequest_Succeeds()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/users", new
        {
            given_name = "Nimal",
            family_name = "Perera",
            email = $"nimal-{Guid.NewGuid():N}@test.local",
            password = "Temp#Pass1234",
            role = "Staff",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_IsA409()
    {
        var email = $"dupe-{Guid.NewGuid():N}@test.local";
        var client = AdminClient();
        object Body(string address) => new { given_name = "A", family_name = "B", email = address, password = "Temp#Pass1234", role = "Staff" };

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/users", Body(email))).StatusCode);
        // Different case, same mailbox.
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/users", Body(email.ToUpperInvariant()))).StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidRequest_IsA400NotA500()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/users", new
        {
            given_name = "Nimal",
            family_name = "Perera",
            email = "not-an-email",
            password = "short",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ValidRequest_Succeeds()
    {
        var response = await AdminClient().PatchAsJsonAsync($"/api/users/{_staffUserId}", new { role = "Auditor", department_id = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteSetup_IsNotA500()
    {
        // An organisation already exists in this test host, so setup itself
        // is refused; the point is that the request model validates at all.
        var response = await factory.CreateClient().PostAsJsonAsync("/api/setup/complete", new
        {
            admin = new { email = "first-admin@test.local", given_name = "First", family_name = "Admin", password = "Temp#Pass1234" },
            organisation = new { name = "Test Organisation" },
        });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
