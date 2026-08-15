using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly CoreGridDbContext _db;

    public DepartmentsController(
        IDepartmentService departmentService,
        CoreGridDbContext db)
    {
        _departmentService = departmentService;
        _db = db;
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
