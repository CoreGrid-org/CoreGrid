using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Users;

// Administrator-only user management (SRS §4.7). Only the first Administrator
// is created unauthenticated, by Setup — every other CoreGrid user is created
// here, by an already-signed-in Administrator, from the dashboard.
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(CoreGridDbContext db, IIdentityDirectory identityDirectory) : ControllerBase
{
    // Search + pagination — added for the Users & Roles admin page. Org
    // scoping is already handled by the global OrganizationId query filter
    // (FR-006), same as every other query against db.Users in this file.
    [HttpGet]
    [Authorize(Roles = $"{nameof(CoreGridRole.Administrator)},{nameof(CoreGridRole.InventoryOfficer)}")]
    public async Task<ActionResult<PagedResult<UserResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.GivenName, pattern) ||
                EF.Functions.ILike(u.FamilyName, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse(u.Id, u.Email, u.GivenName, u.FamilyName, u.Role, u.DepartmentId, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new PagedResult<UserResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // M0 is one full stack per customer organisation (SRS §4.2) — there is
        // always exactly one Organization row once Setup has run, which it
        // must have for this endpoint to be reachable at all ([Authorize]).
        var organization = await db.Organizations.SingleAsync(cancellationToken);

        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var role = request.Role!.Value;

        var externalSubjectId = await identityDirectory.ProvisionUserAsync(
            request.Email,
            request.GivenName,
            request.FamilyName,
            request.Password,
            role,
            cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ExternalSubjectId = externalSubjectId,
            Email = request.Email,
            GivenName = request.GivenName,
            FamilyName = request.FamilyName,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.DepartmentId, user.IsActive, user.CreatedAt));
    }

    // FR-014: change a user's role or department assignment.
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<UserResponse>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.Departments.AsNoTracking().AnyAsync(
                d => d.Id == request.DepartmentId.Value && d.OrganizationId == user.OrganizationId,
                cancellationToken);

            if (!departmentExists)
            {
                return BadRequest(new { message = "Department was not found." });
            }
        }

        user.Role = request.Role!.Value;
        user.DepartmentId = request.DepartmentId;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.DepartmentId, user.IsActive, user.CreatedAt));
    }

    // FR-014: deactivate a user — retained for historical reference, never
    // hard-deleted. Guards against locking the organisation out by
    // deactivating its last active Administrator.
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<UserResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.Role == CoreGridRole.Administrator && user.IsActive)
        {
            var otherActiveAdmins = await db.Users.AsNoTracking().AnyAsync(
                u => u.OrganizationId == user.OrganizationId
                    && u.Role == CoreGridRole.Administrator
                    && u.IsActive
                    && u.Id != user.Id,
                cancellationToken);

            if (!otherActiveAdmins)
            {
                return BadRequest(new { message = "Cannot deactivate the organisation's last active Administrator." });
            }
        }

        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.DepartmentId, user.IsActive, user.CreatedAt));
    }

    // FR-014 (reactivation is the natural inverse of deactivation).
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<UserResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.DepartmentId, user.IsActive, user.CreatedAt));
    }
}
