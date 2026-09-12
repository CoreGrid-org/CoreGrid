using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Assets.Controllers;

// FR-005 / SRS §4.6, Appendix B: asset:read is Staff/Officer/Auditor/Admin
// (Staff restricted to their own department by a service-layer filter, not
// blocked here); asset:create and asset:update are Officer/Admin only,
// matching the frontend's own routing (only Administrator and
// InventoryOfficer ever reach AssetRegisterPage — App.tsx).
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string ManageRoles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}";

    private readonly IAssetService _assetService;

    public AssetsController(
        IAssetService assetService,
        CoreGridDbContext db) : base(db)
    {
        _assetService = assetService;
    }

    // =========================================================
    // GET /api/assets
    // =========================================================

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
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
    [Authorize(Roles = ReadRoles)]
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
    [Authorize(Roles = ManageRoles)]
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
    [Authorize(Roles = ManageRoles)]
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
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message =
                    "Asset could not be updated because of a database conflict."
            });
        }
    }

    // =========================================================
    // PATCH /api/assets/{id}/condition
    // =========================================================

    [HttpPatch("{id:guid}/condition")]
    [Authorize(Roles = ManageRoles)]
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
    // GET /api/assets/{id}/history
    // =========================================================

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<AssetHistoryDto>>> GetAssetHistory(
        Guid id,
        [FromQuery] AssetHistoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var history = await _assetService.GetAssetHistoryAsync(
            currentUser.OrganizationId,
            id,
            parameters);

        if (history is null)
        {
            return NotFound(new
            {
                message = "Asset not found."
            });
        }

        return Ok(history);
    }

    // =========================================================
    // GET /api/assets/qr/{code}
    // =========================================================

    [HttpGet("qr/{code}")]
    [Authorize(Roles = ReadRoles)]
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
    // GET /api/assets/organization-code
    // =========================================================
    //
    // The organisation's short code (e.g. "MOTAHSL") derived from its name —
    // the same prefix used when generating an asset's AssetCode/QrPayload
    // (see AssetService.CreateAssetAsync). Exposed here so the frontend can
    // display the real value in the asset registration form's code preview.

    [HttpGet("organization-code")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<OrganizationCodeDto>> GetOrganizationCode(
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var code = await _assetService.GetOrganizationCodeAsync(
            currentUser.OrganizationId);

        return Ok(new OrganizationCodeDto
        {
            Code = code
        });
    }

}