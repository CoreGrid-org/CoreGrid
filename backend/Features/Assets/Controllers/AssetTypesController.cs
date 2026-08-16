using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/asset-types")]
[Authorize]
public class AssetTypesController : CoreGridControllerBase
{
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
    // POST /api/asset-types/{id}/attributes
    // =========================================================

    [HttpPost("{id:guid}/attributes")]
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

}