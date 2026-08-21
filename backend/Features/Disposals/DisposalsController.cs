using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals.DTOs;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Disposals;

[ApiController]
[Authorize]
public class DisposalsController : CoreGridControllerBase
{
    private readonly IDisposalService _disposalService;

    public DisposalsController(IDisposalService disposalService, CoreGridDbContext db) : base(db)
    {
        _disposalService = disposalService;
    }

    // =========================================================
    // POST /api/assets/{id}/condemn — FR-049 / disposal:request (InventoryOfficer, Administrator)
    // =========================================================
    [HttpPost("api/assets/{id:guid}/condemn")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<CondemnAssetResponse>> CondemnAsset(
        Guid id,
        [FromBody] CondemnAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _disposalService.CondemnAssetAsync(
                currentUser.OrganizationId,
                id,
                request,
                currentUser.Id,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // =========================================================
    // POST /api/disposals — FR-050 / disposal:request (InventoryOfficer, Administrator)
    // =========================================================
    [HttpPost("api/disposals")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<DisposalResponse>> SubmitDisposal(
        [FromBody] SubmitDisposalRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _disposalService.SubmitDisposalRequestAsync(
                currentUser.OrganizationId,
                request,
                currentUser.Id,
                cancellationToken);

            return CreatedAtAction(nameof(GetDisposalById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // =========================================================
    // POST /api/disposals/{id}/approve — FR-051 / disposal:approve (Administrator only)
    // =========================================================
    [HttpPost("api/disposals/{id:guid}/approve")]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<DisposalResponse>> ApproveDisposal(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _disposalService.ApproveDisposalAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                cancellationToken);

            if (result.IsForbidden)
            {
                return StatusCode(403, new
                {
                    message = result.ForbiddenReason ?? "Separation of duties violation: Approver cannot be the requester.",
                    preconditions = result.PreconditionResult
                });
            }

            if (result.IsInvalidState)
            {
                return Conflict(new
                {
                    message = result.InvalidStateReason ?? "Invalid state transition."
                });
            }

            if (!result.Success)
            {
                return UnprocessableEntity(new
                {
                    message = "One or more disposal preconditions failed.",
                    preconditions = result.PreconditionResult
                });
            }

            return Ok(result.DisposalResponse);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // =========================================================
    // GET /api/disposals — list with filters
    // =========================================================
    [HttpGet("api/disposals")]
    public async Task<ActionResult<List<DisposalResponse>>> GetDisposals(
        [FromQuery] DisposalQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.GetDisposalRequestsAsync(
            currentUser.OrganizationId,
            parameters,
            cancellationToken);

        return Ok(result);
    }

    // =========================================================
    // GET /api/disposals/{id} — detail with live precondition checklist
    // =========================================================
    [HttpGet("api/disposals/{id:guid}")]
    public async Task<ActionResult<DisposalResponse>> GetDisposalById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.GetDisposalRequestByIdAsync(
            currentUser.OrganizationId,
            id,
            currentUser.Id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Disposal request with ID {id} not found." });
        }

        return Ok(result);
    }
}
