using System.Net;
using System.Net.Http.Json;
using CoreGrid.Api.Domain;

namespace backend.Tests.Features.Authorization;

// §13.2 / AI-28: the authorisation matrix across all four CoreGrid roles
// plus the agent service principal. Runs through the real pipeline end to
// end (real named policies and role-based [Authorize] attributes, real
// RoleEnrichmentMiddleware re-deriving the role from the Users table and
// detecting the service principal via ServicePrincipal.Is) — only the JWT
// *identity* verification step is swapped for a test double
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
            acquisition_cost = 1000m,
            acquisition_date = "2020-01-01",
            useful_life_years = 5
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
            acquisition_cost = 1000m,
            acquisition_date = "2020-01-01",
            useful_life_years = 5
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Phase 6 (§8) / SEC-ID-10 / AI-28: the agent service principal holds
    // read-only tool permissions and no business permission whatsoever —
    // every mutating route in the app (every POST/PUT/PATCH/DELETE except
    // compute-depreciation, which is deliberately allowed as a pure,
    // read-only computation — see AgentServicePrincipal_ReachesAgentToolsWithoutAUsersRow
    // above) must deny it. Bodies are deliberately minimal ({} or none):
    // ASP.NET Core's authorization middleware runs before model binding, so
    // an empty body can never mask a 403/401 behind a 400 — this proves the
    // authorisation boundary itself, independent of business validity.
    public static IEnumerable<object[]> MutatingRoutes()
    {
        var id = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        (HttpMethod Method, string Path)[] routes =
        [
            (HttpMethod.Post, "/api/assets"),
            (HttpMethod.Put, $"/api/assets/{id}"),
            (HttpMethod.Patch, $"/api/assets/{id}/condition"),
            (HttpMethod.Post, "/api/asset-categories"),
            (HttpMethod.Put, $"/api/asset-categories/{id}"),
            (HttpMethod.Patch, $"/api/asset-categories/{id}/activate"),
            (HttpMethod.Delete, $"/api/asset-categories/{id}"),
            (HttpMethod.Post, "/api/asset-types"),
            (HttpMethod.Put, $"/api/asset-types/{id}"),
            (HttpMethod.Patch, $"/api/asset-types/{id}/activate"),
            (HttpMethod.Delete, $"/api/asset-types/{id}"),
            (HttpMethod.Post, $"/api/asset-types/{id}/attributes"),
            (HttpMethod.Put, $"/api/asset-types/{id}/attributes/{id2}"),
            (HttpMethod.Patch, $"/api/asset-types/{id}/attributes/{id2}/activate"),
            (HttpMethod.Delete, $"/api/asset-types/{id}/attributes/{id2}"),
            (HttpMethod.Patch, $"/api/verification-tasks/{id}/complete"),
            (HttpMethod.Post, "/api/verification-campaigns"),
            (HttpMethod.Put, $"/api/verification-campaigns/{id}"),
            (HttpMethod.Delete, $"/api/verification-campaigns/{id}"),
            (HttpMethod.Post, "/api/verification-tasks/photos"),
            (HttpMethod.Post, $"/api/verification-tasks/{id}/discrepancies"),
            (HttpMethod.Patch, $"/api/discrepancies/{id}/resolve"),
            (HttpMethod.Post, $"/api/assets/{id}/condemn"),
            (HttpMethod.Post, $"/api/assets/{id}/verify"),
            (HttpMethod.Post, "/api/disposals"),
            (HttpMethod.Post, $"/api/disposals/{id}/approve"),
            (HttpMethod.Post, $"/api/disposals/{id}/reject"),
            (HttpMethod.Post, $"/api/disposals/{id}/request-revision"),
            (HttpMethod.Post, "/api/maintenance/photos"),
            (HttpMethod.Post, "/api/maintenance/faults"),
            (HttpMethod.Post, "/api/maintenance"),
            (HttpMethod.Put, $"/api/maintenance/{id}"),
            (HttpMethod.Post, $"/api/maintenance/{id}/approve"),
            (HttpMethod.Post, $"/api/maintenance/{id}/start"),
            (HttpMethod.Post, $"/api/maintenance/{id}/complete"),
            (HttpMethod.Post, $"/api/maintenance/{id}/cancel"),
            (HttpMethod.Post, "/api/transfers"),
            (HttpMethod.Post, $"/api/transfers/{id}/approve"),
            (HttpMethod.Post, $"/api/transfers/{id}/reject"),
            (HttpMethod.Post, $"/api/transfers/{id}/confirm-receipt"),
            (HttpMethod.Post, "/api/agent-workflows"),
            (HttpMethod.Post, $"/api/agent-workflows/{id}/evaluate"),
            (HttpMethod.Post, $"/api/agent-workflows/{id}/run-policy-agent"),
            (HttpMethod.Post, $"/api/agent-workflows/{id}/run-maintenance-agent"),
            (HttpMethod.Patch, $"/api/agent-workflows/{id}/decide"),
            (HttpMethod.Post, "/api/departments"),
            (HttpMethod.Put, $"/api/departments/{id}"),
            (HttpMethod.Patch, $"/api/departments/{id}/deactivate"),
            (HttpMethod.Patch, $"/api/departments/{id}/activate"),
            (HttpMethod.Post, "/api/locations"),
            (HttpMethod.Put, $"/api/locations/{id}"),
            (HttpMethod.Patch, $"/api/locations/{id}/deactivate"),
            (HttpMethod.Patch, $"/api/locations/{id}/activate"),
            (HttpMethod.Post, "/api/organization-policies"),
            (HttpMethod.Put, $"/api/organization-policies/{id}"),
            (HttpMethod.Post, "/api/users"),
            (HttpMethod.Patch, $"/api/users/{id}"),
            (HttpMethod.Patch, $"/api/users/{id}/deactivate"),
            (HttpMethod.Patch, $"/api/users/{id}/activate"),
            (HttpMethod.Patch, $"/api/notifications/{id}/read"),
            (HttpMethod.Patch, "/api/notifications/read-all"),
        ];

        return routes.Select(r => new object[] { r.Method, r.Path });
    }

    [Theory]
    [MemberData(nameof(MutatingRoutes))]
    public async Task MutatingRoute_DeniesServicePrincipal(HttpMethod method, string path)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-ServicePrincipal", "true");

        using var request = new HttpRequestMessage(method, path);
        if (path.EndsWith("/photos", StringComparison.Ordinal))
        {
            // [Consumes("multipart/form-data")] is matched during endpoint
            // selection, before UseAuthorization() runs — a JSON body gets
            // a 415 with no endpoint selected at all, never reaching (or
            // proving anything about) the authorisation check.
            request.Content = new MultipartFormDataContent();
        }
        else if (method != HttpMethod.Delete)
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            $"{method} {path} returned {(int)response.StatusCode} {response.StatusCode} for the agent service principal — expected 401 or 403 (SEC-ID-10/AI-28).");
    }

    // Phase 6 (§8): the rest of Appendix B's named-policy matrix not already
    // exercised above (CanApproveWorkflow, CanResolveDiscrepancy,
    // CanManageConfiguration and CanVerifyAssets each already have a
    // dedicated test earlier in this file). One row per (route, role) pair;
    // `expectForbidden: false` only proves the role clears the authorisation
    // gate (same "NotEqual(Forbidden)" bar the existing Create*_Allows*
    // tests already use), not that the request fully succeeds — downstream
    // 400/404/409s from an intentionally minimal body are expected and fine.
    [Theory]
    // CanReadAssets — Appendix B: Staff, Officer, Auditor, Administrator (+ the agent principal, covered separately above). Every role passes; there is no role left to deny.
    [InlineData("GET", "/api/assets", CoreGridRole.Staff, false)]
    [InlineData("GET", "/api/assets", CoreGridRole.InventoryOfficer, false)]
    [InlineData("GET", "/api/assets", CoreGridRole.Auditor, false)]
    [InlineData("GET", "/api/assets", CoreGridRole.Administrator, false)]
    // CanManageAssets — Officer, Administrator
    [InlineData("POST", "/api/assets", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/assets", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/assets", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/assets", CoreGridRole.Auditor, true)]
    // CanRequestMaintenance — Staff, Officer, Administrator
    [InlineData("POST", "/api/maintenance/faults", CoreGridRole.Staff, false)]
    [InlineData("POST", "/api/maintenance/faults", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/maintenance/faults", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/maintenance/faults", CoreGridRole.Auditor, true)]
    // CanRequestTransfer — Officer, Administrator
    [InlineData("POST", "/api/transfers", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/transfers", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/transfers", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/transfers", CoreGridRole.Auditor, true)]
    // CanApproveTransfer — Administrator only
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.InventoryOfficer, true)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Auditor, true)]
    // CanConfirmReceipt — Officer, Administrator (plan §4.4's documented deviation from Appendix B's Officer-only row)
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/confirm-receipt", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/confirm-receipt", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/confirm-receipt", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/confirm-receipt", CoreGridRole.Auditor, true)]
    // CanApproveTransfer (reject) — SRS §9.4 — same policy as approve, Administrator only
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.InventoryOfficer, true)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/transfers/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Auditor, true)]
    // CanRequestDisposal — Officer, Administrator
    [InlineData("POST", "/api/disposals", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/disposals", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/disposals", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/disposals", CoreGridRole.Auditor, true)]
    // CanApproveDisposal — Administrator only
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.InventoryOfficer, true)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/approve", CoreGridRole.Auditor, true)]
    // CanApproveDisposal (reject) — SRS §9.4 — same policy as approve, Administrator only
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.InventoryOfficer, true)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/disposals/00000000-0000-0000-0000-000000000001/reject", CoreGridRole.Auditor, true)]
    // CanManageMaintenance (amend) — SRS §9.3 — Officer, Administrator
    [InlineData("PUT", "/api/maintenance/00000000-0000-0000-0000-000000000001", CoreGridRole.InventoryOfficer, false)]
    [InlineData("PUT", "/api/maintenance/00000000-0000-0000-0000-000000000001", CoreGridRole.Administrator, false)]
    [InlineData("PUT", "/api/maintenance/00000000-0000-0000-0000-000000000001", CoreGridRole.Staff, true)]
    [InlineData("PUT", "/api/maintenance/00000000-0000-0000-0000-000000000001", CoreGridRole.Auditor, true)]
    // CanVerifyAssets (standalone verify) — SRS §9.2 — Officer, Auditor (+ Administrator, plan §4.4's documented deviation)
    [InlineData("POST", "/api/assets/00000000-0000-0000-0000-000000000001/verify", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/assets/00000000-0000-0000-0000-000000000001/verify", CoreGridRole.Auditor, false)]
    [InlineData("POST", "/api/assets/00000000-0000-0000-0000-000000000001/verify", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/assets/00000000-0000-0000-0000-000000000001/verify", CoreGridRole.Staff, true)]
    // CanManageCampaigns — Auditor, Administrator
    [InlineData("POST", "/api/verification-campaigns", CoreGridRole.Auditor, false)]
    [InlineData("POST", "/api/verification-campaigns", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/verification-campaigns", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/verification-campaigns", CoreGridRole.InventoryOfficer, true)]
    // CanManageUsers — Administrator only
    [InlineData("POST", "/api/users", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/users", CoreGridRole.InventoryOfficer, true)]
    [InlineData("POST", "/api/users", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/users", CoreGridRole.Auditor, true)]
    // CanInitiateWorkflow — Officer, Administrator
    [InlineData("POST", "/api/agent-workflows", CoreGridRole.InventoryOfficer, false)]
    [InlineData("POST", "/api/agent-workflows", CoreGridRole.Administrator, false)]
    [InlineData("POST", "/api/agent-workflows", CoreGridRole.Staff, true)]
    [InlineData("POST", "/api/agent-workflows", CoreGridRole.Auditor, true)]
    // CanReadAuditLog — Auditor, Administrator
    [InlineData("GET", "/api/audit-log", CoreGridRole.Auditor, false)]
    [InlineData("GET", "/api/audit-log", CoreGridRole.Administrator, false)]
    [InlineData("GET", "/api/audit-log", CoreGridRole.Staff, true)]
    [InlineData("GET", "/api/audit-log", CoreGridRole.InventoryOfficer, true)]
    // AgentWorkflowsController.ReadRoles (execution-summary, SRS §9.6) — same
    // Officer/Auditor/Administrator set as GetWorkflows/GetWorkflowById,
    // Staff excluded (§5.9's documented deviation, stricter than Appendix B)
    [InlineData("GET", "/api/agent-workflows/00000000-0000-0000-0000-000000000001/execution-summary", CoreGridRole.InventoryOfficer, false)] // 404, not 403 — passes the role gate
    [InlineData("GET", "/api/agent-workflows/00000000-0000-0000-0000-000000000001/execution-summary", CoreGridRole.Staff, true)]
    public async Task NamedPolicy_MatchesAppendixB(string method, string path, CoreGridRole role, bool expectForbidden)
    {
        var client = ClientAs(role);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        if (expectForbidden)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        else
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
