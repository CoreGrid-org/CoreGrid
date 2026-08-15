using CoreGrid.Api.Data;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : CoreGridControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(
        IDepartmentService departmentService,
        CoreGridDbContext db) : base(db)
    {
        _departmentService = departmentService;
    }

    // GET /api/departments
    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetDepartments(
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var departments =
            await _departmentService.GetDepartmentsAsync(
                currentUser.OrganizationId);

        return Ok(departments);
    }

    // POST /api/departments
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var department = await _departmentService.CreateDepartmentAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return Ok(department);
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
                message = "Department could not be created because of a database conflict."
            });
        }
    }

    // PUT /api/departments/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var department = await _departmentService.UpdateDepartmentAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (department is null)
            {
                return NotFound(new
                {
                    message = "Department not found."
                });
            }

            return Ok(department);
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
                message = "Department could not be updated because of a database conflict."
            });
        }
    }

    // PATCH /api/departments/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<DepartmentDto>> DeactivateDepartment(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, false, cancellationToken);
    }

    // PATCH /api/departments/{id}/activate
    [HttpPatch("{id:guid}/activate")]
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

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var department = await _departmentService.SetDepartmentActiveAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                isActive);

            if (department is null)
            {
                return NotFound(new
                {
                    message = "Department not found."
                });
            }

            return Ok(department);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
