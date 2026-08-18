using System;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Maintenance.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : CoreGridControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        CoreGridDbContext db) : base(db)
    {
        _maintenanceService = maintenanceService;
    }

    [HttpPost("faults")]
    public async Task<ActionResult<MaintenanceRecordDto>> ReportFault(
        [FromBody] ReportFaultRequest request)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.ReportFaultAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            if (record is null)
            {
                return BadRequest(new { message = "Failed to report fault." });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = record.Id },
                record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaintenanceRecordDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var record = await _maintenanceService.GetMaintenanceRecordByIdAsync(
            currentUser.OrganizationId,
            id);

        if (record is null)
        {
            return NotFound(new { message = "Maintenance record not found." });
        }

        return Ok(record);
    }

    // FR-035 - Create maintenance record directly                         
    /// Creates a maintenance record directly (not via a fault report),The caller specifies type (CORRECTIVE / PREVENTIVE) and priority

    [HttpPost]
    [Authorize(Roles = nameof(CoreGridRole.InventoryOfficer))]
    public async Task<ActionResult<MaintenanceRecordDto>> CreateMaintenance(
        [FromBody] CreateMaintenanceRequest request)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.CreateMaintenanceAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            if (record is null)
            {
                return BadRequest(new { message = "Failed to create maintenance record." });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = record.Id },
                record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // FR-036 — Approve maintenance record                                 
    /// Approves a REQUESTED maintenance record: assigns it to a responsible
    /// officer and records an estimated cost. Transitions status:
    /// REQUESTED → APPROVED. Restricted to InventoryOfficer or Administrator.

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<MaintenanceRecordDto>> ApproveMaintenance(
        Guid id,
        [FromBody] ApproveMaintenanceRequest request)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.ApproveMaintenanceAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                id,
                request);

            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    // FR-037 / FR-039 — Start maintenance record                          //
    /// Starts an APPROVED maintenance record: transitions status to
    /// IN_PROGRESS and places the asset into UNDER_MAINTENANCE (FR-039).
    /// The caller must be an InventoryOfficer and must be the assignee
    /// (or an Administrator progressing work on their behalf).

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<MaintenanceRecordDto>> StartMaintenance(Guid id)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.StartMaintenanceAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                id);

            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    // FR-038 / FR-040 — Complete maintenance record                       
    /// Completes an IN_PROGRESS maintenance record (FR-038).
    /// Records actual cost, work performed, completion date and resulting
    /// condition. Returns the asset to ACTIVE (or CONDEMNED for UNSERVICEABLE
    /// — BR2). Recalculates cumulative cost + repair count (FR-040).
    /// Enforces cost-variance tolerance (BR1). Atomic (BR3).
    /// Returns 409 if the record is already COMPLETED (AC1).

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = nameof(CoreGridRole.InventoryOfficer))]
    public async Task<ActionResult<MaintenanceRecordDto>> CompleteMaintenance(
        Guid id,
        [FromBody] CompleteMaintenanceRequest request)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.CompleteMaintenanceAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                id,
                request);

            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("This maintenance record has already been completed"))
        {
            // AC1 — a second completion attempt returns 409 Conflict.
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
