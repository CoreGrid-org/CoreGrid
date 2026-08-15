namespace CoreGrid.Api.Features.Setup;

// Property names here are chosen to match the frontend's existing snake_case,
// British-spelling wire contract (frontend/src/services/setup.ts) once the
// snake_case JSON naming policy in Program.cs converts them at the boundary.
public record SetupStatusResponse(bool NeedsSetup);

public record CompleteSetupRequest(AdminRequest Admin, OrganisationRequest Organisation);

public record AdminRequest(string Email, string GivenName, string FamilyName, string Password);

public record OrganisationRequest(string Name);

public record CompleteSetupResponse(Guid OrganisationId);
