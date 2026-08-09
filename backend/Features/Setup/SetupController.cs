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
        // endpoint refuses to create another one. Further organisations are
        // out of scope for the baseline (SRS's tenant institutions are
        // provisioned by CoreGrid engineering, not self-service).
        if (await db.Organizations.AnyAsync(cancellationToken))
        {
            return Conflict("This CoreGrid instance is already set up.");
        }

        // Creates the ThunderID sub-organisation and the admin's ThunderID
        // account inside it (SRS §4.2, §4.7). Throws until that integration
        // is wired up — see Identity/ThunderIdIdentityDirectory.cs.
        var provisioned = await identityDirectory.ProvisionOrganizationAdministratorAsync(
            request.Organisation.Name,
            request.Admin.Email,
            request.Admin.GivenName,
            request.Admin.FamilyName,
            request.Admin.Password,
            cancellationToken);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            ExternalOrgId = provisioned.ExternalOrgId,
            Name = request.Organisation.Name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ExternalSubjectId = provisioned.ExternalSubjectId,
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
