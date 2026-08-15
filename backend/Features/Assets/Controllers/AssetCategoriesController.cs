using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/asset-categories")]
[Authorize]
public class AssetCategoriesController : ControllerBase
{
    private readonly IAssetCategoryService _assetCategoryService;
    private readonly CoreGridDbContext _db;

    public AssetCategoriesController(
        IAssetCategoryService assetCategoryService,
        CoreGridDbContext db)
    {
        _assetCategoryService = assetCategoryService;
        _db = db;
    }

    // GET /api/asset-categories
    [HttpGet]
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

    private async Task<CoreGrid.Api.Domain.User?> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalSubjectId =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(externalSubjectId))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.ExternalSubjectId == externalSubjectId,
                cancellationToken);
    }
}