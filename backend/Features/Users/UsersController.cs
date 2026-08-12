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
[Authorize(Roles = nameof(CoreGridRole.Administrator))]
public class UsersController(CoreGridDbContext db, IIdentityDirectory identityDirectory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserResponse(u.Id, u.Email, u.GivenName, u.FamilyName, u.Role, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
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

        return Ok(new UserResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.IsActive, user.CreatedAt));
    }
}
