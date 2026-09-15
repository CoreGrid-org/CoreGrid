using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-005 / SRS §4.6: audit:log-read is Auditor/Administrator only, and
// discrepancy *listing/resolution* is only ever reached through the Audit
// page in the frontend (App.tsx routes it to Auditor/Administrator alone —
// Officer's own routes have no discrepancies view) — so those two stay
// AuditRoles-only. Manual *raising* (FR-061) is different: the SRS names it
// an Officer/Flutter action (06-functional-requirements.md FR-061), reached
// from the mobile verification flow, not the web Audit page — so it also
// needs InventoryOfficer, on top of AuditRoles for parity with any future
// web-side raise action.
[ApiController]
[Route("api")]
[Authorize]
public class DiscrepanciesController : CoreGridControllerBase
{
    private const string AuditRoles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string RaiseRoles = $"{AuditRoles},{nameof(CoreGridRole.InventoryOfficer)}";

    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5MB, matches Maintenance's fault-report limit

    private readonly IDiscrepancyService _discrepancyService;
    private readonly Features.Shared.Storage.IFileStorageService _fileStorageService;

    public DiscrepanciesController(
        IDiscrepancyService discrepancyService,
        Features.Shared.Storage.IFileStorageService fileStorageService,
        CoreGridDbContext db) : base(db)
    {
        _discrepancyService = discrepancyService;
        _fileStorageService = fileStorageService;
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

    // FR-061: upload a discrepancy photo to object storage and get back the
    // URL to include in RaiseDiscrepancyRequest.PhotoUrl — same
    // upload-then-reference pattern as Maintenance's POST /api/maintenance/photos.
    [HttpPost("verification-tasks/photos")]
    [Authorize(Roles = RaiseRoles)]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<ActionResult<UploadDiscrepancyPhotoResponse>> UploadPhoto(
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
            var url = await _fileStorageService.UploadAsync("verification", photo.FileName, photo.ContentType, stream, cancellationToken);
            return Ok(new UploadDiscrepancyPhotoResponse { Url = url });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    // FR-061: manual discrepancy raising against a specific task.
    [HttpPost("verification-tasks/{taskId:guid}/discrepancies")]
    [Authorize(Roles = RaiseRoles)]
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
