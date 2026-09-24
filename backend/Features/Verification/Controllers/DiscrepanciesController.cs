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

// Handles discrepancy management and verification photo uploads.
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

    // Uploads a photo associated with a verification discrepancy.
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

    // Raises a discrepancy for a verification task.
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

    //  An Auditor resolves a discrepancy.
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
