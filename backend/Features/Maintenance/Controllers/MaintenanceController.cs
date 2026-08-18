using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : CoreGridControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly CoreGridDbContext _db;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        CoreGridDbContext db) : base(db)
    {
        _maintenanceService = maintenanceService;
        _db = db;
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

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<MaintenanceRecordDto>> CancelMaintenance(
        Guid id,
        [FromBody] CancelMaintenanceRequest request)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var record = await _maintenanceService.CancelMaintenanceAsync(
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaintenanceRecordDto>>> ListMaintenanceRecords(
        [FromQuery] MaintenanceRecordFilter filter)
    {
        var currentUser = await GetCurrentUserAsync(default);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var records = await _maintenanceService.ListMaintenanceRecordsAsync(
            currentUser.OrganizationId,
            filter);

        return Ok(records);
    }

    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> Seed()
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var organization = await _db.Organizations.FirstOrDefaultAsync();
            if (organization == null)
            {
                return BadRequest("No organization found. Please run setup first.");
            }

            var assets = await _db.Assets.Take(5).ToListAsync();
            if (assets.Count == 0)
            {
                return BadRequest("No assets found. Please register some assets first.");
            }

            var user = await _db.Users.FirstOrDefaultAsync();
            var userId = user?.Id;

            // Check if we already have records
            var existingCount = await _db.MaintenanceRecords.CountAsync();
            if (existingCount > 0)
            {
                return Ok(new { message = $"Seeding skipped: {existingCount} records already exist." });
            }

            var records = new List<MaintenanceRecord>();

            // 1. Completed Corrective Maintenance
            records.Add(new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AssetId = assets[0].Id,
                Description = "Replaced faulty battery and clean contacts.",
                ObservedCondition = "UNSERVICEABLE",
                Type = MaintenanceType.CORRECTIVE,
                Priority = MaintenancePriority.HIGH,
                Status = MaintenanceStatus.COMPLETED,
                EstimatedCost = 4500,
                ActualCost = 4200,
                WorkPerformed = "Replaced battery with model XP-900. Cleaned all terminals.",
                CompletionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                ResultingCondition = "GOOD",
                AssigneeId = userId,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                CreatedBy = userId,
                UpdatedBy = userId
            });

            // 2. In Progress Corrective Maintenance
            records.Add(new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AssetId = assets[assets.Count > 1 ? 1 : 0].Id,
                Description = "Repair screen flicker and bezel damage.",
                ObservedCondition = "POOR",
                Type = MaintenanceType.CORRECTIVE,
                Priority = MaintenancePriority.MEDIUM,
                Status = MaintenanceStatus.IN_PROGRESS,
                EstimatedCost = 15000,
                AssigneeId = userId,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId
            });

            // 3. Requested Corrective Maintenance (Awaiting Approval)
            records.Add(new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AssetId = assets[assets.Count > 2 ? 2 : 0].Id,
                Description = "Keyboard keys stuck (A, S, D). Needs replacement or deep clean.",
                ObservedCondition = "POOR",
                Type = MaintenanceType.CORRECTIVE,
                Priority = MaintenancePriority.LOW,
                Status = MaintenanceStatus.REQUESTED,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-5),
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId
            });

            // 4. Approved Preventive Maintenance (Not started yet)
            records.Add(new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AssetId = assets[assets.Count > 3 ? 3 : 0].Id,
                Description = "Annual safety inspection and calibration.",
                ObservedCondition = "GOOD",
                Type = MaintenanceType.PREVENTIVE,
                Priority = MaintenancePriority.MEDIUM,
                Status = MaintenanceStatus.APPROVED,
                EstimatedCost = 8000,
                AssigneeId = userId,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId
            });

            _db.MaintenanceRecords.AddRange(records);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = $"Seeded {records.Count} maintenance records successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = $"Failed to seed: {ex.Message}" });
        }
    }
}
