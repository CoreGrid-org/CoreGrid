using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Assets.Controllers;

// FR-005 / SRS §4.6: broad read (every role needs the category list to
// filter/register assets); management is a configuration action, so it
// uses CanManageConfiguration (Administrator-only) rather than
// CanManageAssets — matches AssetConfigPage's Administrator-only route in
// App.tsx.
[ApiController]
[Route("api/asset-categories")]
[Authorize]
public class AssetCategoriesController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IAssetCategoryService _assetCategoryService;

    public AssetCategoriesController(
        IAssetCategoryService assetCategoryService,
        CoreGridDbContext db) : base(db)
    {
        _assetCategoryService = assetCategoryService;
    }

    // GET /api/asset-categories
    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<AssetCategoryDto>>> GetCategories(
        [FromQuery] PagedQuery query,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var categories = await _assetCategoryService.GetCategoriesAsync(
            currentUser.OrganizationId, query, includeInactive, cancellationToken);

        return Ok(categories);
    }

    // GET /api/asset-categories/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<AssetCategoryDto>> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var category = await _assetCategoryService.GetCategoryByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        return category is null
            ? throw NotFoundException.For(nameof(AssetCategory), id)
            : Ok(category);
    }

    // POST /api/asset-categories
    [HttpPost]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetCategoryDto>> CreateCategory(
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var category = await _assetCategoryService.CreateCategoryAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
    }

    // PUT /api/asset-categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetCategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var category = await _assetCategoryService.UpdateCategoryAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return category is null
            ? throw NotFoundException.For(nameof(AssetCategory), id)
            : Ok(category);
    }

    // DELETE /api/asset-categories/{id}
    //
    // Hard-deletes the category if no AssetType references it; otherwise
    // deactivates it instead (IsActive = false) so existing AssetTypes keep
    // working, while it stops appearing as a choice for new ones.
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var (found, hardDeleted, category) = await _assetCategoryService.DeleteCategoryAsync(
            currentUser.OrganizationId, id, currentUser.Id, cancellationToken);

        if (!found)
        {
            throw NotFoundException.For(nameof(AssetCategory), id);
        }

        return hardDeleted ? NoContent() : Ok(category);
    }

    // PATCH /api/asset-categories/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetCategoryDto>> ActivateCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var category = await _assetCategoryService.SetCategoryActiveAsync(
            currentUser.OrganizationId, id, currentUser.Id, true, cancellationToken);

        return category is null
            ? throw NotFoundException.For(nameof(AssetCategory), id)
            : Ok(category);
    }
}
