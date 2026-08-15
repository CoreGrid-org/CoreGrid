using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly CoreGridDbContext _db;

    public AssetsController(
        IAssetService assetService,
        CoreGridDbContext db)
    {
        _assetService = assetService;
        _db = db;
    }

    // =========================================================
    // GET /api/assets
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetAssets(
        [FromQuery] AssetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _assetService.GetAssetsAsync(
                currentUser.OrganizationId,
                parameters);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // GET /api/assets/{id}
    // =========================================================

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDetailDto>> GetAssetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var asset = await _assetService.GetAssetByIdAsync(
            currentUser.OrganizationId,
            id);

        if (asset is null)
        {
            return NotFound(new
            {
                message = "Asset not found."
            });
        }

        return Ok(asset);
    }

    // =========================================================
    // POST /api/assets
    // =========================================================

    [HttpPost]
    public async Task<ActionResult<AssetDetailDto>> CreateAsset(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var asset = await _assetService.CreateAssetAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return CreatedAtAction(
                nameof(GetAssetById),
                new
                {
                    id = asset.Id
                },
                asset);
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
                message =
                    "Asset could not be created because of a database conflict."
            });
        }
    }

    // =========================================================
    // PUT /api/assets/{id}
    // =========================================================

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssetDetailDto>> UpdateAsset(
        Guid id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var asset = await _assetService.UpdateAssetAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (asset is null)
            {
                return NotFound(new
                {
                    message = "Asset not found."
                });
            }

            return Ok(asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // PATCH /api/assets/{id}/condition
    // =========================================================

    [HttpPatch("{id:guid}/condition")]
    public async Task<IActionResult> UpdateCondition(
        Guid id,
        [FromBody] UpdateAssetConditionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _assetService.UpdateConditionAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Asset not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // GET /api/assets/qr/{code}
    // =========================================================

    [HttpGet("qr/{code}")]
    public async Task<ActionResult<AssetDetailDto>> GetAssetByQrCode(
        string code,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var asset = await _assetService.GetAssetByQrCodeAsync(
            currentUser.OrganizationId,
            code);

        if (asset is null)
        {
            return NotFound(new
            {
                message = "Asset not found."
            });
        }

        return Ok(asset);
    }

    // =========================================================
    // CURRENT USER
    // Same pattern used by existing MeController
    // =========================================================

    private async Task<CoreGrid.Api.Domain.User?> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalSubjectId =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(externalSubjectId))
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