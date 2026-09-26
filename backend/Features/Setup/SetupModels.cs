using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Setup;

// Defines the request and response models for initial application setup.
public record SetupStatusResponse(bool NeedsSetup);

public record CompleteSetupRequest([Required] AdminRequest Admin, [Required] OrganisationRequest Organisation);

public record AdminRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string GivenName,
    [Required, MaxLength(100)] string FamilyName,
    [Required, MinLength(8), MaxLength(200)] string Password);

public record OrganisationRequest([Required, MaxLength(200)] string Name);

public record CompleteSetupResponse(Guid OrganisationId);
