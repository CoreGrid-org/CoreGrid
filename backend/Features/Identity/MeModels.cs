using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Identity;

public record MeResponse(
    Guid Id,
    string Email,
    string GivenName,
    string FamilyName,
    CoreGridRole Role,
    bool IsActive,
    Guid OrganizationId,
    string OrganizationName);
