using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Http;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreGrid.Api.Features.Maintenance.Controllers;

// FR-005 / SRS §4.6: maintenance:request is Staff/Officer/Administrator
// (not Auditor) — CanRequestMaintenance; maintenance:manage (approve/start/
// cancel) is Officer/Administrator — CanManageMaintenance; read endpoints
// stay broad (all four roles have a legitimate reason to see a maintenance
// record — Staff who reported it, Auditor for reports), with Staff
// restricted to their own department (B14). CreateMaintenance and
// CompleteMaintenance stay InventoryOfficer-only, deliberately stricter
// than CanManageMaintenance's Officer+Admin — an existing behaviour kept
// and recorded (plan §5.4), not widened by this refactor.
[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

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
    [Authorize(Policy = Policies.CanRequestMaintenance)]
    [EnableRateLimiting(RateLimitPolicies.PhotoUpload)]
    [RequestSizeLimit(PhotoUploadValidator.MaxSizeBytes)]
    public async Task<ActionResult<UploadPhotoResponse>> UploadPhoto(
        IFormFile photo, CancellationToken cancellationToken)
    {
        var validation = await PhotoUploadValidator.ValidateAsync(photo, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(nameof(photo), validation.Error!);
        }

        await using var stream = photo.OpenReadStream();
        var key = await _fileStorageService.UploadPrivateAsync("maintenance", photo.FileName, photo.ContentType, stream, cancellationToken);
        return Ok(new UploadPhotoResponse { Url = key });
    }

    [HttpPost("faults")]
    [Authorize(Policy = Policies.CanRequestMaintenance)]
    public async Task<ActionResult<MaintenanceRecordDto>> ReportFault(
        [FromBody] ReportFaultRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.ReportFaultAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = record!.Id }, record);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<MaintenanceRecordDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.GetMaintenanceRecordByIdAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), id, cancellationToken);

        return record is null
            ? throw NotFoundException.For(nameof(MaintenanceRecord), id)
            : Ok(record);
    }

    // FR-035 — creates a maintenance record directly (not via a fault
    // report); the caller specifies type (CORRECTIVE / PREVENTIVE) and
    // priority.
    [HttpPost]
    [Authorize(Roles = nameof(CoreGridRole.InventoryOfficer))]
    public async Task<ActionResult<MaintenanceRecordDto>> CreateMaintenance(
        [FromBody] CreateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.CreateMaintenanceAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = record!.Id }, record);
    }

    // FR-036 — Approves a REQUESTED maintenance record: assigns it to a
    // responsible officer and records an estimated cost. Transitions
    // status: REQUESTED → APPROVED.
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.CanManageMaintenance)]
    public async Task<ActionResult<MaintenanceRecordDto>> ApproveMaintenance(
        Guid id, [FromBody] ApproveMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.ApproveMaintenanceAsync(
            currentUser.OrganizationId, currentUser.Id, id, request, cancellationToken);

        return Ok(record);
    }

    // FR-037 / FR-039 — Starts an APPROVED maintenance record: transitions
    // status to IN_PROGRESS and places the asset into UNDER_MAINTENANCE.
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Policies.CanManageMaintenance)]
    public async Task<ActionResult<MaintenanceRecordDto>> StartMaintenance(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.StartMaintenanceAsync(
            currentUser.OrganizationId, currentUser.Id, id, cancellationToken);

        return Ok(record);
    }

    // FR-038 / FR-040 — Completes an IN_PROGRESS maintenance record.
    // Records actual cost, work performed, completion date and resulting
    // condition. Returns the asset to ACTIVE (or CONDEMNED for
    // UNSERVICEABLE — BR2). Recalculates cumulative cost + repair count
    // (FR-040). Enforces cost-variance tolerance (BR1). Atomic (BR3).
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = nameof(CoreGridRole.InventoryOfficer))]
    public async Task<ActionResult<MaintenanceRecordDto>> CompleteMaintenance(
        Guid id, [FromBody] CompleteMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.CompleteMaintenanceAsync(
            currentUser.OrganizationId, currentUser.Id, id, request, cancellationToken);

        return Ok(record);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CanManageMaintenance)]
    public async Task<ActionResult<MaintenanceRecordDto>> CancelMaintenance(
        Guid id, [FromBody] CancelMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var record = await _maintenanceService.CancelMaintenanceAsync(
            currentUser.OrganizationId, currentUser.Id, id, request, cancellationToken);

        return Ok(record);
    }

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<MaintenanceRecordDto>>> ListMaintenanceRecords(
        [FromQuery] MaintenanceRecordFilter filter, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var records = await _maintenanceService.ListMaintenanceRecordsAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), filter, cancellationToken);

        return Ok(records);
    }
}
