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

    // ------------------------------------------------------------------ //
    // FR-035 — Create maintenance record directly                         //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Creates a maintenance record directly (not via a fault report).
    /// The caller specifies type (CORRECTIVE / PREVENTIVE) and priority
    /// explicitly. Restricted to InventoryOfficer.
    /// </summary>
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

    // ------------------------------------------------------------------ //
    // FR-036 — Approve maintenance record                                 //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Approves a REQUESTED maintenance record: assigns it to a responsible
    /// officer and records an estimated cost. Transitions status:
    /// REQUESTED → APPROVED. Restricted to InventoryOfficer or Administrator.
    /// </summary>
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
}
