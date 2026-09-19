using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Transfers.DTOs;
using CoreGrid.Api.Features.Transfers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Transfers.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public class TransfersController : CoreGridControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService, CoreGridDbContext db) : base(db)
    {
        _transferService = transferService;
    }

    // POST /api/transfers — FR-044 / CanRequestTransfer (Officer, Administrator)
    [HttpPost]
    [Authorize(Policy = Policies.CanRequestTransfer)]
    public async Task<ActionResult<TransferResponse>> InitiateTransfer(
        [FromBody] InitiateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.InitiateTransferAsync(
            currentUser.OrganizationId, request, currentUser.Id, cancellationToken);

        return CreatedAtAction(nameof(GetTransferById), new { id = result.Id }, result);
    }

    // POST /api/transfers/{id}/approve — FR-045 / CanApproveTransfer (Administrator)
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.CanApproveTransfer)]
    public async Task<ActionResult<TransferResponse>> ApproveTransfer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.ApproveTransferAsync(
            currentUser.OrganizationId, id, currentUser.Id, cancellationToken);

        return Ok(result);
    }

    // POST /api/transfers/{id}/reject — SRS §9.4 / FR-045 / CanApproveTransfer (Administrator)
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.CanApproveTransfer)]
    public async Task<ActionResult<TransferResponse>> RejectTransfer(
        Guid id,
        [FromBody] RejectTransferRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.RejectTransferAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return Ok(result);
    }

    // POST /api/transfers/{id}/confirm-receipt — FR-046 / CanConfirmReceipt (InventoryOfficer, Administrator)
    [HttpPost("{id:guid}/confirm-receipt")]
    [Authorize(Policy = Policies.CanConfirmReceipt)]
    public async Task<ActionResult<TransferResponse>> ConfirmReceipt(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.ConfirmReceiptAsync(
            currentUser.OrganizationId, id, currentUser.Id, currentUser.Role, currentUser.DepartmentId, cancellationToken);

        return Ok(result);
    }

    // GET /api/transfers — authenticated + org-scoped list with status/department filters
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransferResponse>>> GetTransfers(
        [FromQuery] TransferQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.GetTransfersAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), parameters, cancellationToken);

        return Ok(result);
    }

    // GET /api/transfers/{id} — detail, authenticated + org-scoped
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransferResponse>> GetTransferById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.GetTransferByIdAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), id, cancellationToken);

        return result is null
            ? throw NotFoundException.For(nameof(AssetTransfer), id)
            : Ok(result);
    }

    // GET /api/assets/{assetId}/transfers — FR-047: Complete transfer history for an asset
    [HttpGet("/api/assets/{assetId:guid}/transfers")]
    public async Task<ActionResult<PagedResult<TransferResponse>>> GetTransferHistoryForAsset(
        Guid assetId,
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.GetTransferHistoryForAssetAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), assetId, query, cancellationToken);

        return Ok(result);
    }
}
