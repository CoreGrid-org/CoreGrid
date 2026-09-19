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

// FR-005 / SRS §4.6: asset type/attribute definitions are catalog
// configuration, not asset instances — reads are broad (all four roles need
// them to populate forms/filters), writes use CanManageConfiguration
// (Administrator-only), matching the frontend's own routing (only
// Administrator ever reaches AssetConfigPage — App.tsx; InventoryOfficer
// gets asset create/edit but not /assets/config).
[ApiController]
[Route("api/asset-types")]
[Authorize]
public class AssetTypesController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IAssetTypeService _assetTypeService;

    public AssetTypesController(
        IAssetTypeService assetTypeService,
        CoreGridDbContext db) : base(db)
    {
        _assetTypeService = assetTypeService;
    }

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<AssetTypeDto>>> GetAssetTypes(
        [FromQuery] PagedQuery query,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetTypes = await _assetTypeService.GetAssetTypesAsync(
            currentUser.OrganizationId, query, includeInactive, cancellationToken);

        return Ok(assetTypes);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<AssetTypeDto>> GetAssetTypeById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetType = await _assetTypeService.GetAssetTypeByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        return assetType is null
            ? throw NotFoundException.For(nameof(AssetType), id)
            : Ok(assetType);
    }

    // Deliberately unpaginated (plan §12.3) — the dynamic asset form needs
    // every attribute definition for the type, not a page of them.
    [HttpGet("{id:guid}/attributes")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<List<AssetAttributeDefinitionDto>>> GetAttributeDefinitions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetType = await _assetTypeService.GetAssetTypeByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        if (assetType is null)
        {
            throw NotFoundException.For(nameof(AssetType), id);
        }

        var attributes = await _assetTypeService.GetAttributeDefinitionsAsync(currentUser.OrganizationId, id, cancellationToken);
        return Ok(attributes);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetTypeDto>> CreateAssetType(
        [FromBody] CreateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetType = await _assetTypeService.CreateAssetTypeAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetAssetTypeById), new { id = assetType.Id }, assetType);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetTypeDto>> UpdateAssetType(
        Guid id,
        [FromBody] UpdateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetType = await _assetTypeService.UpdateAssetTypeAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return assetType is null
            ? throw NotFoundException.For(nameof(AssetType), id)
            : Ok(assetType);
    }

    // Hard-deletes the type if no Asset references it; otherwise
    // deactivates it instead so existing Assets keep displaying it.
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<IActionResult> DeleteAssetType(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var (found, hardDeleted, assetType) = await _assetTypeService.DeleteAssetTypeAsync(
            currentUser.OrganizationId, id, currentUser.Id, cancellationToken);

        if (!found)
        {
            throw NotFoundException.For(nameof(AssetType), id);
        }

        return hardDeleted ? NoContent() : Ok(assetType);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetTypeDto>> ActivateAssetType(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var assetType = await _assetTypeService.SetAssetTypeActiveAsync(
            currentUser.OrganizationId, id, currentUser.Id, true, cancellationToken);

        return assetType is null
            ? throw NotFoundException.For(nameof(AssetType), id)
            : Ok(assetType);
    }

    [HttpPost("{id:guid}/attributes")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> CreateAttributeDefinition(
        Guid id,
        [FromBody] CreateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var definition = await _assetTypeService.CreateAttributeDefinitionAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        if (definition is null)
        {
            throw NotFoundException.For(nameof(AssetType), id);
        }

        return CreatedAtAction(nameof(GetAttributeDefinitions), new { id }, definition);
    }

    [HttpPut("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> UpdateAttributeDefinition(
        Guid id,
        Guid attributeId,
        [FromBody] UpdateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var definition = await _assetTypeService.UpdateAttributeDefinitionAsync(
            currentUser.OrganizationId, id, attributeId, currentUser.Id, request, cancellationToken);

        return definition is null
            ? throw NotFoundException.For(nameof(AssetAttributeDefinition), attributeId)
            : Ok(definition);
    }

    // Hard-deletes the definition if no AssetAttributeValue references it;
    // otherwise deactivates it instead so existing Assets keep displaying
    // their previously stored value for it.
    [HttpDelete("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<IActionResult> DeleteAttributeDefinition(
        Guid id,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var (found, hardDeleted, definition) = await _assetTypeService.DeleteAttributeDefinitionAsync(
            currentUser.OrganizationId, id, attributeId, currentUser.Id, cancellationToken);

        if (!found)
        {
            throw NotFoundException.For(nameof(AssetAttributeDefinition), attributeId);
        }

        return hardDeleted ? NoContent() : Ok(definition);
    }

    [HttpPatch("{id:guid}/attributes/{attributeId:guid}/activate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<AssetAttributeDefinitionDto>> ActivateAttributeDefinition(
        Guid id,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var definition = await _assetTypeService.SetAttributeDefinitionActiveAsync(
            currentUser.OrganizationId, id, attributeId, currentUser.Id, true, cancellationToken);

        return definition is null
            ? throw NotFoundException.For(nameof(AssetAttributeDefinition), attributeId)
            : Ok(definition);
    }
}
