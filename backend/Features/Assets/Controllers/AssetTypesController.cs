using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Assets.Controllers;

// FR-005 / SRS §4.6: asset type/attribute definitions are catalog
// configuration, not asset instances — reads are broad (all four roles need
// them to populate forms/filters), writes are Administrator-only, matching
// the frontend's own routing (only Administrator ever reaches
// AssetConfigPage — App.tsx; InventoryOfficer gets asset create/edit but not
// /assets/config).
[ApiController]
[Route("api/asset-types")]
[Authorize]
public class AssetTypesController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string ManageRoles = nameof(CoreGridRole.Administrator);

    private readonly IAssetTypeService _assetTypeService;

    public AssetTypesController(
        IAssetTypeService assetTypeService,
        CoreGridDbContext db) : base(db)
    {
        _assetTypeService = assetTypeService;
    }

    // =========================================================
    // GET /api/asset-types
    // =========================================================

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<List<AssetTypeDto>>> GetAssetTypes(
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var assetTypes =
            await _assetTypeService.GetAssetTypesAsync(
                currentUser.OrganizationId);

        return Ok(assetTypes);
    }

    // =========================================================
    // GET /api/asset-types/{id}
    // =========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<AssetTypeDto>> GetAssetTypeById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var assetType =
            await _assetTypeService.GetAssetTypeByIdAsync(
                currentUser.OrganizationId,
                id);

        if (assetType is null)
        {
            return NotFound(new
            {
                message = "Asset type not found."
            });
        }

        return Ok(assetType);
    }

    // =========================================================
    // GET /api/asset-types/{id}/attributes
    // =========================================================

    [HttpGet("{id:guid}/attributes")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<List<AssetAttributeDefinitionDto>>>
        GetAttributeDefinitions(
            Guid id,
            CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var assetType =
            await _assetTypeService.GetAssetTypeByIdAsync(
                currentUser.OrganizationId,
                id);

        if (assetType is null)
        {
            return NotFound(new
            {
                message = "Asset type not found."
            });
        }

        var attributes =
            await _assetTypeService.GetAttributeDefinitionsAsync(
                currentUser.OrganizationId,
                id);

        return Ok(attributes);
    }

    // =========================================================
    // POST /api/asset-types
    // =========================================================

    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetTypeDto>> CreateAssetType(
        [FromBody] CreateAssetTypeRequest request,
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
            var assetType = await _assetTypeService.CreateAssetTypeAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return CreatedAtAction(
                nameof(GetAssetTypeById),
                new
                {
                    id = assetType.Id
                },
                assetType);
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
                message = "Asset type could not be created because of a database conflict."
            });
        }
    }

    // =========================================================
    // PUT /api/asset-types/{id}
    // =========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetTypeDto>> UpdateAssetType(
        Guid id,
        [FromBody] UpdateAssetTypeRequest request,
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
            var assetType = await _assetTypeService.UpdateAssetTypeAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (assetType is null)
            {
                return NotFound(new
                {
                    message = "Asset type not found."
                });
            }

            return Ok(assetType);
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
                message = "Asset type could not be updated because of a database conflict."
            });
        }
    }

    // =========================================================
    // DELETE /api/asset-types/{id}
    //
    // Hard-deletes the type if no Asset references it; otherwise
    // deactivates it instead so existing Assets keep displaying it.
    // =========================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteAssetType(
        Guid id,
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
            var (found, hardDeleted, assetType) = await _assetTypeService.DeleteAssetTypeAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id);

            if (!found)
            {
                return NotFound(new
                {
                    message = "Asset type not found."
                });
            }

            return hardDeleted
                ? NoContent()
                : Ok(assetType);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Asset type could not be deleted because of a database conflict."
            });
        }
    }

    // =========================================================
    // PATCH /api/asset-types/{id}/activate
    // =========================================================

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetTypeDto>> ActivateAssetType(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var assetType = await _assetTypeService.SetAssetTypeActiveAsync(
            currentUser.OrganizationId,
            id,
            currentUser.Id,
            true);

        if (assetType is null)
        {
            return NotFound(new
            {
                message = "Asset type not found."
            });
        }

        return Ok(assetType);
    }

    // =========================================================
    // POST /api/asset-types/{id}/attributes
    // =========================================================

    [HttpPost("{id:guid}/attributes")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> CreateAttributeDefinition(
        Guid id,
        [FromBody] CreateAssetAttributeDefinitionRequest request,
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
            var definition = await _assetTypeService.CreateAttributeDefinitionAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (definition is null)
            {
                return NotFound(new
                {
                    message = "Asset type not found."
                });
            }

            return CreatedAtAction(
                nameof(GetAttributeDefinitions),
                new
                {
                    id
                },
                definition);
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
                message = "Attribute could not be created because of a database conflict."
            });
        }
    }

    // =========================================================
    // PUT /api/asset-types/{id}/attributes/{attributeId}
    // =========================================================

    [HttpPut("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> UpdateAttributeDefinition(
        Guid id,
        Guid attributeId,
        [FromBody] UpdateAssetAttributeDefinitionRequest request,
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
            var definition = await _assetTypeService.UpdateAttributeDefinitionAsync(
                currentUser.OrganizationId,
                id,
                attributeId,
                currentUser.Id,
                request);

            if (definition is null)
            {
                return NotFound(new
                {
                    message = "Asset type or attribute not found."
                });
            }

            return Ok(definition);
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
                message = "Attribute could not be updated because of a database conflict."
            });
        }
    }

    // =========================================================
    // DELETE /api/asset-types/{id}/attributes/{attributeId}
    //
    // Hard-deletes the definition if no AssetAttributeValue references it;
    // otherwise deactivates it instead so existing Assets keep displaying
    // their previously stored value for it.
    // =========================================================

    [HttpDelete("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteAttributeDefinition(
        Guid id,
        Guid attributeId,
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
            var (found, hardDeleted, definition) = await _assetTypeService.DeleteAttributeDefinitionAsync(
                currentUser.OrganizationId,
                id,
                attributeId,
                currentUser.Id);

            if (!found)
            {
                return NotFound(new
                {
                    message = "Asset type or attribute not found."
                });
            }

            return hardDeleted
                ? NoContent()
                : Ok(definition);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Attribute could not be deleted because of a database conflict."
            });
        }
    }

    // =========================================================
    // PATCH /api/asset-types/{id}/attributes/{attributeId}/activate
    // =========================================================

    [HttpPatch("{id:guid}/attributes/{attributeId:guid}/activate")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> ActivateAttributeDefinition(
        Guid id,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        var currentUser =
            await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var definition = await _assetTypeService.SetAttributeDefinitionActiveAsync(
            currentUser.OrganizationId,
            id,
            attributeId,
            currentUser.Id,
            true);

        if (definition is null)
        {
            return NotFound(new
            {
                message = "Asset type or attribute not found."
            });
        }

        return Ok(definition);
    }

}