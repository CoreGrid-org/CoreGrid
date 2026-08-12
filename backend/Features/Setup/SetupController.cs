using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Setup;

// A CoreGrid deployment "needs setup" until its first organisation exists.
// There is no authenticated user yet at this point, so these endpoints are
// deliberately the only unauthenticated write path in the API, and only
// ever do anything while zero organisations exist (see Complete below).
[ApiController]
[Route("api/setup")]
public class SetupController(CoreGridDbContext db, IIdentityDirectory identityDirectory) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SetupStatusResponse>> Status(CancellationToken cancellationToken)
    {
        var needsSetup = !await db.Organizations.AnyAsync(cancellationToken);
        return Ok(new SetupStatusResponse(needsSetup));
    }

    [HttpPost("complete")]
    public async Task<ActionResult<CompleteSetupResponse>> Complete(
        CompleteSetupRequest request,
        CancellationToken cancellationToken)
    {
        // Setup is a one-time operation — once any organisation exists, this
        // endpoint refuses to create another one. This is deliberate, not a
        // missing feature: in M0, CoreGrid is self-hosted once per customer
        // organisation (SRS §2.4, §4.2), so a given deployment only ever has
        // one Organization. M1 (SRS §17) lifts this restriction in favour of
        // self-service signup — this check is the one line that changes.
        if (await db.Organizations.AnyAsync(cancellationToken))
        {
            return Conflict("This CoreGrid instance is already set up.");
        }

        // Creates the admin's ThunderID account (SRS §4.7). This deployment's
        // ThunderID instance is single-tenant too — the Organization row
        // below is CoreGrid's own record of this deployment's customer,
        // with no ThunderID-side counterpart.
        var externalSubjectId = await identityDirectory.ProvisionUserAsync(
            request.Admin.Email,
            request.Admin.GivenName,
            request.Admin.FamilyName,
            request.Admin.Password,
            CoreGridRole.Administrator,
            cancellationToken);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Organisation.Name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ExternalSubjectId = externalSubjectId,
            Email = request.Admin.Email,
            GivenName = request.Admin.GivenName,
            FamilyName = request.Admin.FamilyName,
            Role = CoreGridRole.Administrator,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Organizations.Add(organization);
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new CompleteSetupResponse(organization.Id));
    }
}
