using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
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
    [HttpGet]
    [Authorize(Roles = $"{nameof(CoreGridRole.Administrator)},{nameof(CoreGridRole.InventoryOfficer)}")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserResponse(u.Id, u.Email, u.GivenName, u.FamilyName, u.Role, u.DepartmentId, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = nameof(CoreGridRole.Administrator))]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // M0 is one full stack per customer organisation (SRS §4.2) — there is
        // always exactly one Organization row once Setup has run, which it
        // must have for this endpoint to be reachable at all ([Authorize]).
        var organization = await db.Organizations.SingleAsync(cancellationToken);

        var externalSubjectId = await identityDirectory.ProvisionUserAsync(
            request.Email,
            request.GivenName,
            request.FamilyName,
            request.Password,
            request.Role,
            cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ExternalSubjectId = externalSubjectId,
            Email = request.Email,
            GivenName = request.GivenName,
            FamilyName = request.FamilyName,
            Role = request.Role,
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

        user.Role = request.Role;
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
