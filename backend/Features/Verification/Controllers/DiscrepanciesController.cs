using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Storage;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-005 / SRS §4.6: audit:log-read is Auditor/Administrator only, and
// discrepancy *listing/resolution* is only ever reached through the Audit
// page in the frontend (App.tsx routes it to Auditor/Administrator alone —
// Officer's own routes have no discrepancies view) — so those two use
// CanResolveDiscrepancy (Auditor/Administrator). Manual *raising* (FR-061)
// is different: the SRS names it an Officer/Flutter action
// (06-functional-requirements.md FR-061), reached from the mobile
// verification flow, not the web Audit page — so it stays an inline role
// list (Auditor/Administrator/InventoryOfficer) rather than a named
// policy, since no Appendix B policy covers exactly that combination.
[ApiController]
[Route("api")]
[Authorize]
public class DiscrepanciesController : CoreGridControllerBase
{
    private const string AuditRoles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string RaiseRoles = $"{AuditRoles},{nameof(CoreGridRole.InventoryOfficer)}";

    private readonly IDiscrepancyService _discrepancyService;
    private readonly IFileStorageService _fileStorageService;

    public DiscrepanciesController(
        IDiscrepancyService discrepancyService,
        IFileStorageService fileStorageService,
        CoreGridDbContext db) : base(db)
    {
        _discrepancyService = discrepancyService;
        _fileStorageService = fileStorageService;
    }

    // GET /api/discrepancies?campaignId=&onlyOpen=
    [HttpGet("discrepancies")]
    [Authorize(Roles = AuditRoles)]
    public async Task<ActionResult<PagedResult<DiscrepancyDto>>> GetDiscrepancies(
        [FromQuery] DiscrepancyQueryParameters query,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _discrepancyService.GetDiscrepanciesAsync(currentUser.OrganizationId, query, cancellationToken));
    }

    // FR-061: upload a discrepancy photo to object storage and get back the
    // URL to include in RaiseDiscrepancyRequest.PhotoUrl — same
    // upload-then-reference pattern as Maintenance's POST /api/maintenance/photos.
    [HttpPost("verification-tasks/photos")]
    [Authorize(Roles = RaiseRoles)]
    [RequestSizeLimit(PhotoUploadValidator.MaxSizeBytes)]
    public async Task<ActionResult<UploadDiscrepancyPhotoResponse>> UploadPhoto(
        IFormFile photo, CancellationToken cancellationToken)
    {
        var validation = await PhotoUploadValidator.ValidateAsync(photo, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(nameof(photo), validation.Error!);
        }

        await using var stream = photo.OpenReadStream();
        var url = await _fileStorageService.UploadAsync("verification", photo.FileName, photo.ContentType, stream, cancellationToken);
        return Ok(new UploadDiscrepancyPhotoResponse { Url = url });
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

        var discrepancy = await _discrepancyService.RaiseManualAsync(
            currentUser.OrganizationId, taskId, currentUser.Id, request, cancellationToken);

        return discrepancy is null
            ? throw NotFoundException.For(nameof(VerificationTask), taskId)
            : Ok(discrepancy);
    }

    // FR-062: An Auditor resolves a discrepancy.
    [HttpPatch("discrepancies/{id:guid}/resolve")]
    [Authorize(Policy = Policies.CanResolveDiscrepancy)]
    public async Task<ActionResult<DiscrepancyDto>> ResolveDiscrepancy(
        Guid id,
        [FromBody] ResolveDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var discrepancy = await _discrepancyService.ResolveAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return discrepancy is null
            ? throw NotFoundException.For(nameof(Discrepancy), id)
            : Ok(discrepancy);
    }
}
