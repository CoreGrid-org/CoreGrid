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

    // FR-005 correction, 2026-09-12: AssetCategoriesController/AssetTypesController
    // writes were blanket [Authorize] (any role) — now Administrator-only,
    // matching AssetConfigPage's Administrator-only route in App.tsx.
    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.Forbidden)]
    public async Task CreateAssetCategory_EnforcesAdministratorOnly(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).PostAsJsonAsync("/api/asset-categories", new { name = "Test", code = "TST" });
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task CreateAssetCategory_AllowsAdministratorThroughTheGate()
    {
        var response = await ClientAs(CoreGridRole.Administrator)
            .PostAsJsonAsync("/api/asset-categories", new { name = "Test", code = "TST" });
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // FR-005 correction, 2026-09-12: DepartmentsController/LocationsController
    // writes were blanket [Authorize] — now Administrator-only (config:manage).
    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.Forbidden)]
    public async Task CreateDepartment_EnforcesAdministratorOnly(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).PostAsJsonAsync("/api/departments", new { name = "Test", code = "TST" });
        Assert.Equal(expected, response.StatusCode);
    }

    // FR-005 correction, 2026-09-12: VerificationTasksController.CompleteTask
    // had no role restriction at all — Staff has no role in verification
    // per SRS §4.6's asset:verify row and is now excluded.
    [Fact]
    public async Task CompleteVerificationTask_ExcludesStaff()
    {
        var response = await ClientAs(CoreGridRole.Staff)
            .PatchAsync($"/api/verification-tasks/{Guid.NewGuid()}/complete", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(CoreGridRole.InventoryOfficer)]
    [InlineData(CoreGridRole.Auditor)]
    [InlineData(CoreGridRole.Administrator)]
    public async Task CompleteVerificationTask_AllowsVerificationRolesThroughTheGate(CoreGridRole role)
    {
        var response = await ClientAs(role)
            .PatchAsync($"/api/verification-tasks/{Guid.NewGuid()}/complete", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // FR-005 correction, 2026-09-12: DiscrepanciesController.GetDiscrepancies
    // had no role restriction — now Auditor/Administrator only, matching
    // audit:log-read and the fact Officer's own routes have no discrepancies view.
    [Theory]
    [InlineData(CoreGridRole.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.InventoryOfficer, HttpStatusCode.Forbidden)]
    [InlineData(CoreGridRole.Auditor, HttpStatusCode.OK)]
    [InlineData(CoreGridRole.Administrator, HttpStatusCode.OK)]
    public async Task GetDiscrepancies_EnforcesAuditRolesOnly(CoreGridRole role, HttpStatusCode expected)
    {
        var response = await ClientAs(role).GetAsync("/api/discrepancies");
        Assert.Equal(expected, response.StatusCode);
    }

    // FR-005 correction, 2026-09-12: MaintenanceController's demo/dev "seed"
    // endpoint was [AllowAnonymous] — reachable unauthenticated. Now
    // Administrator-only; an unauthenticated caller gets 401, not through to
    // the seeding logic at all.
    [Fact]
    public async Task MaintenanceSeed_RejectsUnauthenticatedCaller()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/maintenance/seed", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MaintenanceSeed_RejectsNonAdministrator()
    {
        var response = await ClientAs(CoreGridRole.InventoryOfficer).PostAsync("/api/maintenance/seed", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
