using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Setup;

// Property names here are chosen to match the frontend's existing snake_case,
// British-spelling wire contract (frontend/src/services/setup.ts) once the
// snake_case JSON naming policy in Program.cs converts them at the boundary.
public record SetupStatusResponse(bool NeedsSetup);

public record CompleteSetupRequest([property: Required] AdminRequest Admin, [property: Required] OrganisationRequest Organisation);

public record AdminRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(100)] string GivenName,
    [property: Required, MaxLength(100)] string FamilyName,
    [property: Required, MinLength(8), MaxLength(200)] string Password);

public record OrganisationRequest([property: Required, MaxLength(200)] string Name);

public record CompleteSetupResponse(Guid OrganisationId);
