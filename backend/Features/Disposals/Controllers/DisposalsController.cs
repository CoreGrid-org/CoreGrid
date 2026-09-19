using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals.DTOs;
using CoreGrid.Api.Features.Disposals.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Scoping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Disposals.Controllers;

[ApiController]
[Authorize]
public class DisposalsController : CoreGridControllerBase
{
    private readonly IDisposalService _disposalService;

    public DisposalsController(IDisposalService disposalService, CoreGridDbContext db) : base(db)
    {
        _disposalService = disposalService;
    }

    // POST /api/assets/{id}/condemn — FR-049 / CanRequestDisposal (InventoryOfficer, Administrator)
    [HttpPost("api/assets/{id:guid}/condemn")]
    [Authorize(Policy = Policies.CanRequestDisposal)]
    public async Task<ActionResult<CondemnAssetResponse>> CondemnAsset(
        Guid id,
        [FromBody] CondemnAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.CondemnAssetAsync(
            currentUser.OrganizationId, id, request, currentUser.Id, cancellationToken);

        return Ok(result);
    }

    // POST /api/disposals — FR-050 / CanRequestDisposal (InventoryOfficer, Administrator)
    [HttpPost("api/disposals")]
    [Authorize(Policy = Policies.CanRequestDisposal)]
    public async Task<ActionResult<DisposalResponse>> SubmitDisposal(
        [FromBody] SubmitDisposalRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.SubmitDisposalRequestAsync(
            currentUser.OrganizationId, request, currentUser.Id, cancellationToken);

        return CreatedAtAction(nameof(GetDisposalById), new { id = result.Id }, result);
    }

    // POST /api/disposals/{id}/approve — FR-051 / CanApproveDisposal (Administrator only)
    [HttpPost("api/disposals/{id:guid}/approve")]
    [Authorize(Policy = Policies.CanApproveDisposal)]
    public async Task<ActionResult<DisposalResponse>> ApproveDisposal(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.ApproveDisposalAsync(
            currentUser.OrganizationId, id, currentUser.Id, cancellationToken);

        return Ok(result);
    }

    // POST /api/disposals/{id}/request-revision — FR-053 / CanApproveDisposal (Administrator only)
    [HttpPost("api/disposals/{id:guid}/request-revision")]
    [Authorize(Policy = Policies.CanApproveDisposal)]
    public async Task<ActionResult<DisposalResponse>> RequestDisposalRevision(
        Guid id,
        [FromBody] RequestDisposalRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.RequestDisposalRevisionAsync(
            currentUser.OrganizationId, id, currentUser.Id, request.Comments, cancellationToken);

        return Ok(result);
    }

    // GET /api/disposals — list with filters, Staff scoped to own department (B14)
    [HttpGet("api/disposals")]
    public async Task<ActionResult<PagedResult<DisposalResponse>>> GetDisposals(
        [FromQuery] DisposalQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.GetDisposalRequestsAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), parameters, cancellationToken);

        return Ok(result);
    }

    // GET /api/disposals/{id} — detail with live precondition checklist
    [HttpGet("api/disposals/{id:guid}")]
    public async Task<ActionResult<DisposalResponse>> GetDisposalById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _disposalService.GetDisposalRequestByIdAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), id, currentUser.Id, cancellationToken);

        return result is null
            ? throw NotFoundException.For(nameof(DisposalRequest), id)
            : Ok(result);
    }
}
