using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Assets.Controllers;

// FR-005 / SRS §4.6: same split as AssetTypesController — broad read,
// Administrator-only writes (matches AssetConfigPage's Administrator-only
// route in App.tsx).
[ApiController]
[Route("api/asset-categories")]
[Authorize]
public class AssetCategoriesController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string ManageRoles = nameof(CoreGridRole.Administrator);

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
    public async Task<ActionResult<List<AssetCategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var categories =
            await _assetCategoryService.GetCategoriesAsync(
                currentUser.OrganizationId);

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

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var category =
            await _assetCategoryService.GetCategoryByIdAsync(
                currentUser.OrganizationId,
                id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Asset category not found."
            });
        }

        return Ok(category);
    }

    // POST /api/asset-categories
    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetCategoryDto>> CreateCategory(
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var category = await _assetCategoryService.CreateCategoryAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return CreatedAtAction(
                nameof(GetCategoryById),
                new
                {
                    id = category.Id
                },
                category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Asset category could not be created because of a database conflict."
            });
        }
    }

    // PUT /api/asset-categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetCategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var category = await _assetCategoryService.UpdateCategoryAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (category is null)
            {
                return NotFound(new
                {
                    message = "Asset category not found."
                });
            }

            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Asset category could not be updated because of a database conflict."
            });
        }
    }

    // DELETE /api/asset-categories/{id}
    //
    // Hard-deletes the category if no AssetType references it; otherwise
    // deactivates it instead (IsActive = false) so existing AssetTypes keep
    // working, while it stops appearing as a choice for new ones.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var (found, hardDeleted, category) = await _assetCategoryService.DeleteCategoryAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id);

            if (!found)
            {
                return NotFound(new
                {
                    message = "Asset category not found."
                });
            }

            return hardDeleted
                ? NoContent()
                : Ok(category);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Asset category could not be deleted because of a database conflict."
            });
        }
    }

    // PATCH /api/asset-categories/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetCategoryDto>> ActivateCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var category = await _assetCategoryService.SetCategoryActiveAsync(
            currentUser.OrganizationId,
            id,
            currentUser.Id,
            true);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Asset category not found."
            });
        }

        return Ok(category);
    }

}