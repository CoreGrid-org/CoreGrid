using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Transfers.DTOs;
using CoreGrid.Api.Features.Transfers.Services;

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
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<TransferResponse>> InitiateTransfer(
        [FromBody] InitiateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _transferService.InitiateTransferAsync(
                currentUser.OrganizationId,
                request,
                currentUser.Id,
                cancellationToken);

            return CreatedAtAction(nameof(GetTransferById), new { id = result.Id }, result);
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

    // POST /api/transfers/{id}/approve — FR-045 / CanApproveTransfer (Administrator)
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<TransferResponse>> ApproveTransfer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _transferService.ApproveTransferAsync(
                currentUser.OrganizationId,
                id,
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

    // POST /api/transfers/{id}/confirm-receipt — FR-046 / CanConfirmReceipt (InventoryOfficer, Administrator)
    [HttpPost("{id:guid}/confirm-receipt")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<TransferResponse>> ConfirmReceipt(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var result = await _transferService.ConfirmReceiptAsync(
                currentUser.OrganizationId,
                id,
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

    // GET /api/transfers — authenticated + org-scoped list with status/department filters
    [HttpGet]
    public async Task<ActionResult<List<TransferResponse>>> GetTransfers(
        [FromQuery] TransferQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _transferService.GetTransfersAsync(
            currentUser.OrganizationId,
            parameters,
            cancellationToken);

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
            currentUser.OrganizationId,
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Transfer with ID {id} not found." });
        }

        return Ok(result);
    }
}
