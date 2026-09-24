using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Api;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using backend.Tests.Features.Authorization;

namespace backend.Tests.Features.Shared;

// Phase 6 (§8): NFR-11 (under-posting a mandatory value-type field returns
// 400 with a field-level error, not a silent default) and NFR-14/§5.4
// (every 4xx/5xx shares one error envelope, and a 500 never leaks the
// underlying exception's own message to the client).
public class ValidationAndErrorEnvelopeTests : IClassFixture<CoreGridWebApplicationFactory>, IAsyncLifetime
{
    private readonly CoreGridWebApplicationFactory _factory;
    private readonly Guid _orgId = Guid.NewGuid();
    private string _adminSubject = string.Empty;

    public ValidationAndErrorEnvelopeTests(CoreGridWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var db = _factory.CreateDbContext();
        db.Organizations.Add(new Organization { Id = _orgId, Name = "Test Org — ValidationAndErrorEnvelopeTests" });

        _adminSubject = $"validation-test-admin-{Guid.NewGuid():N}";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = _adminSubject,
            Email = $"admin-{Guid.NewGuid():N}@test.local", GivenName = "Test", FamilyName = "Admin",
            Role = CoreGridRole.Administrator, IsActive = true, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Sub", _adminSubject);
        return client;
    }

    [Fact]
    public async Task ComputeDepreciation_UnderPosted_Returns400WithFieldErrors()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/agent-tools/compute-depreciation", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", envelope.GetProperty("code").GetString());
        Assert.True(envelope.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any(), "expected at least one field-level error for the three missing mandatory fields.");
    }

    [Fact]
    public async Task InitiateTransfer_UnderPosted_Returns400WithFieldErrors()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/transfers", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", envelope.GetProperty("code").GetString());
        Assert.True(envelope.TryGetProperty("errors", out var errors));
        var fieldNames = errors.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(fieldNames, name => name.Contains("AssetId", StringComparison.OrdinalIgnoreCase) || name.Contains("asset_id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EveryValidationErrorResponse_CarriesTheSharedEnvelopeShape()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/departments", new { });

        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(envelope.TryGetProperty("message", out _));
        Assert.True(envelope.TryGetProperty("code", out _));
        Assert.True(envelope.TryGetProperty("correlation_id", out _));
    }

    // NFR-14: a 500 (any exception ApiExceptionFilter's own typed branches
    // don't recognise) must never put the underlying exception's own
    // message into the client-visible response. Exercised directly against
    // the filter — deterministic, rather than hunting for a code path that
    // happens to throw one today.
    [Fact]
    public void ApiExceptionFilter_UnhandledException_NeverLeaksTheExceptionMessage()
    {
        const string internalDetail = "Npgsql: column \"internal_secret_column\" does not exist at connection string user=coregrid_internal";

        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException(internalDetail)
        };

        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        filter.OnException(exceptionContext);

        var result = Assert.IsType<ObjectResult>(exceptionContext.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);

        var envelope = Assert.IsType<ErrorEnvelope>(result.Value);
        Assert.Equal("An unexpected error occurred.", envelope.Message);
        Assert.DoesNotContain("internal_secret_column", envelope.Message);
        Assert.DoesNotContain("coregrid_internal", envelope.Message);
        Assert.True(exceptionContext.ExceptionHandled);
    }

    [Fact]
    public void ApiExceptionFilter_NotFoundException_MapsTo404WithoutLeakingInternals()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = NotFoundException.For(nameof(Asset), Guid.NewGuid())
        };

        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        filter.OnException(exceptionContext);

        var result = Assert.IsType<ObjectResult>(exceptionContext.Result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);

        var envelope = Assert.IsType<ErrorEnvelope>(result.Value);
        Assert.Equal("not_found", envelope.Code);
    }
}
