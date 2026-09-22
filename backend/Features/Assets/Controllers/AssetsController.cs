using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Scoping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Assets.Controllers;

// Manages asset operations.
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : CoreGridControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(
        IAssetService assetService,
        CoreGridDbContext db) : base(db)
    {
        _assetService = assetService;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanReadAssets)]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetAssets(
        [FromQuery] AssetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _assetService.GetAssetsAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), parameters, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CanReadAssets)]
    public async Task<ActionResult<AssetDetailDto>> GetAssetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var asset = await _assetService.GetAssetByIdAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), id, cancellationToken);

        return asset is null
            ? throw NotFoundException.For(nameof(Asset), id)
            : Ok(asset);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageAssets)]
    public async Task<ActionResult<AssetDetailDto>> CreateAsset(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var asset = await _assetService.CreateAssetAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetAssetById), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageAssets)]
    public async Task<ActionResult<AssetDetailDto>> UpdateAsset(
        Guid id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var asset = await _assetService.UpdateAssetAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return asset is null
            ? throw NotFoundException.For(nameof(Asset), id)
            : Ok(asset);
    }

    [HttpPatch("{id:guid}/condition")]
    [Authorize(Policy = Policies.CanManageAssets)]
    public async Task<IActionResult> UpdateCondition(
        Guid id,
        [FromBody] UpdateAssetConditionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var updated = await _assetService.UpdateConditionAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        if (!updated)
        {
            throw NotFoundException.For(nameof(Asset), id);
        }

        return NoContent();
    }

    // Verifies the physical status of an asset.
    [HttpPost("{id:guid}/verify")]
    [Authorize(Policy = Policies.CanVerifyAssets)]
    public async Task<ActionResult<AssetVerificationResultDto>> VerifyAsset(
        Guid id,
        [FromBody] VerifyAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _assetService.VerifyAssetAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return result is null
            ? throw NotFoundException.For(nameof(Asset), id)
            : Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = Policies.CanReadAssets)]
    public async Task<ActionResult<PagedResult<AssetHistoryDto>>> GetAssetHistory(
        Guid id,
        [FromQuery] AssetHistoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var history = await _assetService.GetAssetHistoryAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), id, parameters, cancellationToken);

        return history is null
            ? throw NotFoundException.For(nameof(Asset), id)
            : Ok(history);
    }

    [HttpGet("qr/{code}")]
    [Authorize(Policy = Policies.CanReadAssets)]
    public async Task<ActionResult<AssetDetailDto>> GetAssetByQrCode(
        string code,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var asset = await _assetService.GetAssetByQrCodeAsync(
            currentUser.OrganizationId, DepartmentScope.For(currentUser), code, cancellationToken);

        return asset is null
            ? throw new NotFoundException($"Asset with code '{code}' was not found.")
            : Ok(asset);
    }

    // Returns the organization code used for asset identification.
    [HttpGet("organization-code")]
    [Authorize(Policy = Policies.CanReadAssets)]
    public async Task<ActionResult<OrganizationCodeDto>> GetOrganizationCode(
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var code = await _assetService.GetOrganizationCodeAsync(currentUser.OrganizationId, cancellationToken);
        return Ok(new OrganizationCodeDto { Code = code });
    }
}
