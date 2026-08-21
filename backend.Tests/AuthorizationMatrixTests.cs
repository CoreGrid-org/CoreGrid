using System.Net;
using System.Net.Http.Json;
using CoreGrid.Api.Domain;

namespace backend.Tests.Features.Authorization;

// §13.2 / AI-28: the authorisation matrix across all four CoreGrid roles
// plus the agent service principal. Runs through the real pipeline end to
// end (real [Authorize(Roles=...)] policies, real RoleEnrichmentMiddleware
// re-deriving the role from the Users table, real AgentToolsAuthMiddleware)
// — only the JWT *identity* verification step is swapped for a test double
// (TestAuthHandler); everything downstream of "who is this" is untouched.
public class AuthorizationMatrixTests : IClassFixture<CoreGridWebApplicationFactory>, IAsyncLifetime
{
    private readonly CoreGridWebApplicationFactory _factory;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Dictionary<CoreGridRole, string> _subjectsByRole = new();
    private string _inactiveUserSubject = string.Empty;

    public AuthorizationMatrixTests(CoreGridWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var db = _factory.CreateDbContext();

        db.Organizations.Add(new Organization { Id = _orgId, Name = "Test Org — AuthorizationMatrixTests" });

        foreach (var role in new[] { CoreGridRole.Staff, CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator })
        {
            var subject = $"authz-test-{role}-{Guid.NewGuid():N}";
            _subjectsByRole[role] = subject;
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = subject,
                Email = $"{role}-{Guid.NewGuid():N}@test.local", GivenName = "Test", FamilyName = role.ToString(),
                Role = role, IsActive = true, CreatedAt = DateTimeOffset.UtcNow
            });
        }

        _inactiveUserSubject = $"authz-test-inactive-{Guid.NewGuid():N}";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = _inactiveUserSubject,
            Email = $"inactive-{Guid.NewGuid():N}@test.local", GivenName = "Test", FamilyName = "Inactive",
            Role = CoreGridRole.Administrator, IsActive = false, CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(CoreGridRole role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", _subjectsByRole[role]);
        return client;
    }

    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.OK)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.OK)]
    [InlineData(CoreGridRole.Administrator, HttpStatusCode.OK)]
    public async Task GetAgentWorkflows_EnforcesFR069(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).GetAsync("/api/agent-workflows");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Administrator, HttpStatusCode.NotFound)] // passes the role gate; 404 because the workflow doesn't exist
    public async Task DecideAgentWorkflow_EnforcesAI14_AdministratorOnly(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).PatchAsync(
            $"/api/agent-workflows/{Guid.NewGuid()}/decide",
            JsonContent.Create(new { decision = "APPROVE", reason = "Test reason of sufficient length." }));
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.NotFound)] // passes the role gate; 404 because the discrepancy doesn't exist
    [InlineData(CoreGridRole.Administrator, HttpStatusCode.NotFound)]
    public async Task ResolveDiscrepancy_EnforcesFR062AC1(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).PatchAsync(
            $"/api/discrepancies/{Guid.NewGuid()}/resolve",
            JsonContent.Create(new { resolution_type = "NO_ACTION", resolution_explanation = "Sufficiently long justification text." }));
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.OK)]
    [InlineData(CoreGridRole.Administrator, HttpStatusCode.OK)]
    public async Task GetDashboardCharts_EnforcesFR082(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).GetAsync("/api/dashboard/charts");
        Assert.Equal(expected, response.StatusCode);
    }

    // FR-009: a deactivated user is denied even with an otherwise-valid identity.
    [Fact]
    public async Task DeactivatedUser_IsRejectedRegardlessOfRole()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", _inactiveUserSubject);

        var response = await client.GetAsync("/api/agent-workflows");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AI-28: the agent service principal reaches /api/agent-tools/* without
    // any CoreGrid Users row at all — client-credentials claims are enough.
    [Fact]
    public async Task AgentServicePrincipal_ReachesAgentToolsWithoutAUsersRow()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-ServicePrincipal", "true");

        var response = await client.PostAsJsonAsync("/api/agent-tools/compute-depreciation", new
        {
            acquisitionCost = 1000m,
            acquisitionDate = "2020-01-01",
            usefulLifeYears = 5
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // A human user, by contrast, is not treated as a service principal even
    // when hitting the same tool routes — same middleware, opposite claim shape.
    [Fact]
    public async Task HumanUser_CanAlsoReachAgentToolsAsThemselves()
    {
        var response = await ClientAs(CoreGridRole.Administrator).PostAsJsonAsync("/api/agent-tools/compute-depreciation", new
        {
            acquisitionCost = 1000m,
            acquisitionDate = "2020-01-01",
            usefulLifeYears = 5
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
