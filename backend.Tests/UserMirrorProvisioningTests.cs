using System.Net;
using System.Text.Json;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Authorization;

// FR-004: create or refresh the local Users mirror from token claims on the
// first authenticated request of a session. Runs through the real pipeline
// (real RoleEnrichmentMiddleware) via GET /api/me, which is [Authorize]-only
// (no role requirement) and returns exactly the mirrored fields this exists
// to keep in sync.
public class UserMirrorProvisioningTests : IClassFixture<CoreGridWebApplicationFactory>, IAsyncLifetime
{
    private readonly CoreGridWebApplicationFactory _factory;
    private Guid _orgId;

    public UserMirrorProvisioningTests(CoreGridWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // xUnit creates a fresh instance of this class per test method but
    // shares the one IClassFixture<CoreGridWebApplicationFactory> (and so
    // the same InMemory database) across all of them — seed the single
    // Organization only once and reuse its id, matching the "exactly one
    // Organization" invariant TryCreateFromClaimsAsync itself relies on.
    public async Task InitializeAsync()
    {
        using var db = _factory.CreateDbContext();
        var existing = await db.Organizations.FirstOrDefaultAsync();
        if (existing is not null)
        {
            _orgId = existing.Id;
            return;
        }

        _orgId = Guid.NewGuid();
        db.Organizations.Add(new Organization { Id = _orgId, Name = "Test Org — UserMirrorProvisioningTests" });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FirstRequest_WithFullClaims_CreatesTheMirrorRow()
    {
        var subject = $"provisioning-test-{Guid.NewGuid():N}";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", subject);
        client.DefaultRequestHeaders.Add("Test-Email", "new.user@test.local");
        client.DefaultRequestHeaders.Add("Test-GivenName", "New");
        client.DefaultRequestHeaders.Add("Test-FamilyName", "User");
        client.DefaultRequestHeaders.Add("Test-Role", nameof(CoreGridRole.Auditor));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var me = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("new.user@test.local", me.RootElement.GetProperty("email").GetString());
        Assert.Equal("New", me.RootElement.GetProperty("given_name").GetString());
        Assert.Equal("User", me.RootElement.GetProperty("family_name").GetString());
        Assert.Equal("Auditor", me.RootElement.GetProperty("role").GetString());
        Assert.Equal(_orgId, me.RootElement.GetProperty("organization_id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(me.RootElement.GetProperty("organization_name").GetString()));

        using var db = _factory.CreateDbContext();
        var stored = await db.Users.SingleAsync(u => u.ExternalSubjectId == subject);
        Assert.Equal(CoreGridRole.Auditor, stored.Role);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task FirstRequest_MissingClaims_FailsClosedWith401_NotByGuessingARole()
    {
        var subject = $"provisioning-test-incomplete-{Guid.NewGuid():N}";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", subject);
        // No email/given_name/family_name/roles claims supplied.

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var db = _factory.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(u => u.ExternalSubjectId == subject));
    }

    [Fact]
    public async Task ExistingUser_TokenEmailChanged_RefreshesProfileButNeverRole()
    {
        var subject = $"provisioning-test-refresh-{Guid.NewGuid():N}";
        using (var seedDb = _factory.CreateDbContext())
        {
            seedDb.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = _orgId,
                ExternalSubjectId = subject,
                Email = "old.address@test.local",
                GivenName = "Old",
                FamilyName = "Name",
                Role = CoreGridRole.Staff,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await seedDb.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", subject);
        client.DefaultRequestHeaders.Add("Test-Email", "new.address@test.local");
        // A stale/forged "roles" claim must never override CoreGrid's own mirror.
        client.DefaultRequestHeaders.Add("Test-Role", nameof(CoreGridRole.Administrator));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var me = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("new.address@test.local", me.RootElement.GetProperty("email").GetString());
        Assert.Equal("Staff", me.RootElement.GetProperty("role").GetString());

        using var db = _factory.CreateDbContext();
        var stored = await db.Users.SingleAsync(u => u.ExternalSubjectId == subject);
        Assert.Equal("new.address@test.local", stored.Email);
        Assert.Equal(CoreGridRole.Staff, stored.Role);
    }
}
