using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-005 / SRS §4.6: audit:log-read is Auditor/Administrator only, and
// discrepancy handling is only ever reached through the Audit page in the
// frontend (App.tsx routes it to Auditor/Administrator alone — Officer's
// own routes have no discrepancies view) — so both read and manual raise
// get the same restriction as the already-gated Resolve action below.
[ApiController]
[Route("api")]
[Authorize]
public class DiscrepanciesController : CoreGridControllerBase
{
    private const string AuditRoles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IDiscrepancyService _discrepancyService;

    public DiscrepanciesController(
        IDiscrepancyService discrepancyService,
        CoreGridDbContext db) : base(db)
    {
        _discrepancyService = discrepancyService;
    }

    // GET /api/discrepancies?campaignId=&onlyOpen=
    [HttpGet("discrepancies")]
    [Authorize(Roles = AuditRoles)]
    public async Task<ActionResult<List<DiscrepancyDto>>> GetDiscrepancies(
        [FromQuery] Guid? campaignId,
        [FromQuery] bool onlyOpen,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _discrepancyService.GetDiscrepanciesAsync(currentUser.OrganizationId, campaignId, onlyOpen));
    }

    // FR-061: manual discrepancy raising against a specific task.
    [HttpPost("verification-tasks/{taskId:guid}/discrepancies")]
    [Authorize(Roles = AuditRoles)]
    public async Task<ActionResult<DiscrepancyDto>> RaiseDiscrepancy(
        Guid taskId,
        [FromBody] RaiseDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var discrepancy = await _discrepancyService.RaiseManualAsync(
                currentUser.OrganizationId,
                taskId,
                currentUser.Id,
                request);

            if (discrepancy is null) return NotFound(new { message = "Verification task not found." });

            return Ok(discrepancy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // FR-062: An Auditor resolves a discrepancy.
    [HttpPatch("discrepancies/{id:guid}/resolve")]
    [Authorize(Roles = AuditRoles)]
    public async Task<ActionResult<DiscrepancyDto>> ResolveDiscrepancy(
        Guid id,
        [FromBody] ResolveDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var discrepancy = await _discrepancyService.ResolveAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (discrepancy is null) return NotFound(new { message = "Discrepancy not found." });

            return Ok(discrepancy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
