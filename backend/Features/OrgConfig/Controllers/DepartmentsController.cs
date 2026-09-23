using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

// Handles department management and retrieval.
[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IDepartmentService _departmentService;

    public DepartmentsController(
        IDepartmentService departmentService,
        CoreGridDbContext db) : base(db)
    {
        _departmentService = departmentService;
    }

    // GET /api/departments
    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<DepartmentDto>>> GetDepartments(
        [FromQuery] PagedQuery query,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var departments = await _departmentService.GetDepartmentsAsync(
            currentUser.OrganizationId, query, includeInactive, cancellationToken);

        return Ok(departments);
    }

    // POST /api/departments
    [HttpPost]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var department = await _departmentService.CreateDepartmentAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return Ok(department);
    }

    // PUT /api/departments/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var department = await _departmentService.UpdateDepartmentAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return department is null
            ? throw NotFoundException.For(nameof(Department), id)
            : Ok(department);
    }

    // PATCH /api/departments/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<DepartmentDto>> DeactivateDepartment(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, false, cancellationToken);
    }

    // PATCH /api/departments/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<DepartmentDto>> ActivateDepartment(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, true, cancellationToken);
    }

    private async Task<ActionResult<DepartmentDto>> SetActive(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var department = await _departmentService.SetDepartmentActiveAsync(
            currentUser.OrganizationId, id, currentUser.Id, isActive, cancellationToken);

        return department is null
            ? throw NotFoundException.For(nameof(Department), id)
            : Ok(department);
    }
}
