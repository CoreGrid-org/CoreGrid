using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Identity;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
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
public class UsersController(CoreGridDbContext db, IIdentityDirectory identityDirectory) : CoreGridControllerBase(db)
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<User, object?>>> SortMap =
        new Dictionary<string, Expression<Func<User, object?>>>
        {
            ["email"] = u => u.Email,
            ["givenName"] = u => u.GivenName,
            ["familyName"] = u => u.FamilyName,
            ["createdAt"] = u => u.CreatedAt,
        };

    // Search + pagination — added for the Users & Roles admin page. Org
    // scoping is already handled by the global OrganizationId query filter
    // (FR-006), same as every other query against db.Users in this file.
    // A deliberate deviation from Appendix B's Administrator-only
    // user:manage (RoleGroups.ManageUsers) is kept inline here, not as a
    // named policy: InventoryOfficer also needs this one read for the
    // maintenance/transfer assignee picker (plan §12.4).
    [HttpGet]
    [Authorize(Roles = $"{nameof(CoreGridRole.Administrator)},{nameof(CoreGridRole.InventoryOfficer)}")]
    public async Task<ActionResult<PagedResult<UserResponse>>> List(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var users = Db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.GivenName, pattern) ||
                EF.Functions.ILike(u.FamilyName, pattern));
        }

        var sorted = users.ApplySort(query, SortMap, defaultSortKey: "createdAt");

        var result = await sorted.ToPagedResultAsync(query, ToResponseExpression, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageUsers)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

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
            OrganizationId = currentUser.OrganizationId,
            ExternalSubjectId = externalSubjectId,
            Email = request.Email,
            GivenName = request.GivenName,
            FamilyName = request.FamilyName,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Db.Users.Add(user);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    // FR-014: change a user's role or department assignment.
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Policies.CanManageUsers)]
    public async Task<ActionResult<UserResponse>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), id);

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await Db.Departments.AsNoTracking().AnyAsync(
                d => d.Id == request.DepartmentId.Value && d.OrganizationId == user.OrganizationId,
                cancellationToken);

            if (!departmentExists)
            {
                throw new ValidationException(nameof(request.DepartmentId), "Department was not found.");
            }
        }

        var newRole = request.Role!.Value;

        // Appendix B (CanManageUsers): the organisation's last active
        // Administrator can't be locked out — a role demotion is exactly
        // as disabling as a deactivation, so the same guard applies here
        // (plan §5.1), not only to Deactivate below.
        if (user.Role == CoreGridRole.Administrator && user.IsActive && newRole != CoreGridRole.Administrator)
        {
            await EnsureNotLastActiveAdministratorAsync(user, cancellationToken);
        }

        user.Role = newRole;
        user.DepartmentId = request.DepartmentId;
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    // FR-014: deactivate a user — retained for historical reference, never
    // hard-deleted. Guards against locking the organisation out by
    // deactivating its last active Administrator, and (Appendix B) against
    // an Administrator deactivating their own account.
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.CanManageUsers)]
    public async Task<ActionResult<UserResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), id);

        if (user.Id == currentUser.Id)
        {
            throw new ForbiddenException("You cannot deactivate your own account.");
        }

        if (user.Role == CoreGridRole.Administrator && user.IsActive)
        {
            await EnsureNotLastActiveAdministratorAsync(user, cancellationToken);
        }

        user.IsActive = false;
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    // FR-014 (reactivation is the natural inverse of deactivation).
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageUsers)]
    public async Task<ActionResult<UserResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), id);

        user.IsActive = true;
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    private async Task EnsureNotLastActiveAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        var otherActiveAdmins = await Db.Users.AsNoTracking().AnyAsync(
            u => u.OrganizationId == user.OrganizationId
                && u.Role == CoreGridRole.Administrator
                && u.IsActive
                && u.Id != user.Id,
            cancellationToken);

        if (!otherActiveAdmins)
        {
            throw new BusinessRuleException(
                "Cannot remove the organisation's last active Administrator.",
                "last_active_administrator");
        }
    }

    // The one UserResponse mapper (previously built inline four times) —
    // an Expression so List's EF projection can translate it to SQL, with
    // ToResponse compiled from the same definition for the other three
    // actions, which map an already-materialized entity in memory.
    private static readonly Expression<Func<User, UserResponse>> ToResponseExpression =
        u => new UserResponse(u.Id, u.Email, u.GivenName, u.FamilyName, u.Role, u.DepartmentId, u.IsActive, u.CreatedAt);

    private static readonly Func<User, UserResponse> ToResponse = ToResponseExpression.Compile();
}
