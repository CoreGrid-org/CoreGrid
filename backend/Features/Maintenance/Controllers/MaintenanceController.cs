using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Maintenance.Controllers;

// FR-005 / SRS §4.6: maintenance:request is Staff/Officer/Administrator
// (not Auditor); maintenance:manage (cancel) is Officer/Administrator; read
// endpoints stay broad (all four roles have a legitimate reason to see a
// maintenance record — Staff who reported it, Auditor for reports).
[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : CoreGridControllerBase
{
    private const string RequestRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}";
    private const string ManageRoles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}";
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5MB, matches ReportFaultPage's stated limit

    private readonly IMaintenanceService _maintenanceService;
    private readonly IFileStorageService _fileStorageService;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        IFileStorageService fileStorageService,
        CoreGridDbContext db) : base(db)
    {
        _maintenanceService = maintenanceService;
        _fileStorageService = fileStorageService;
    }

    // FR-034: upload a fault-report photo to Cloudflare R2 as a private
    // object and get back the object key to include in
    // ReportFaultRequest/CreateMaintenanceRequest's PhotoUrl — a separate
    // step from submitting the fault report itself so the JSON endpoints
    // below don't need to change to multipart/form-data. The key alone is
    // never independently useful — a read of the owning record (GetById /
    // list, both role-gated) is what turns it into a short-lived, signed
    // URL, so the raw upload response can't be used to bypass that gate.
    [HttpPost("photos")]
    [Authorize(Roles = RequestRoles)]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<ActionResult<UploadPhotoResponse>> UploadPhoto(
        IFormFile photo, CancellationToken cancellationToken)
    {
        if (photo.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (photo.Length > MaxPhotoSizeBytes)
        {
            return BadRequest(new { message = "Photo must be 5MB or smaller." });
        }

        if (!AllowedPhotoContentTypes.Contains(photo.ContentType))
        {
            return BadRequest(new { message = "Only JPEG, PNG or WebP photos are accepted." });
        }

        try
        {
            await using var stream = photo.OpenReadStream();
            var key = await _fileStorageService.UploadPrivateAsync("maintenance", photo.FileName, photo.ContentType, stream, cancellationToken);
            return Ok(new UploadPhotoResponse { Url = key });
        }
        catch (InvalidOperationException ex)
        {
            // Cloudflare R2 not configured yet, or the upload itself failed.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [HttpPost("faults")]
    [Authorize(Roles = RequestRoles)]
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
    [Authorize(Roles = ReadRoles)]
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
    [Authorize(Roles = ManageRoles)]
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
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<MaintenanceRecordDto>>> ListMaintenanceRecords(
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
}
