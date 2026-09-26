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

// Handles user management operations.
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

  // Returns users with search and pagination.
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
        // Gets the validated user role.
        var role = request.Role!.Value;

        // Emails are unique across every organisation (Users.Email index), so
        // check all of them, not just this tenant's, before creating the
        // ThunderID account; otherwise the database insert fails afterwards
        // and leaves an orphaned identity behind.
        var email = request.Email.Trim();
        var normalisedEmail = email.ToLowerInvariant();
        if (await Db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email.ToLower() == normalisedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email address already exists.", "email_taken");
        }

        var externalSubjectId = await identityDirectory.ProvisionUserAsync(
            email,
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
            Email = email,
            GivenName = request.GivenName,
            FamilyName = request.FamilyName,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Db.Users.Add(user);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    // change a user's role or department assignment.
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
    // Deactivates a user without deleting the account.
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

    // Reactivates a user.
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

    // Defines the shared user response mapping.
    private static readonly Expression<Func<User, UserResponse>> ToResponseExpression =
        u => new UserResponse(u.Id, u.Email, u.GivenName, u.FamilyName, u.Role, u.DepartmentId, u.IsActive, u.CreatedAt);

    private static readonly Func<User, UserResponse> ToResponse = ToResponseExpression.Compile();
}
