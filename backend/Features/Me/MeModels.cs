using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Me;

public record MeResponse(Guid Id, string Email, string GivenName, string FamilyName, CoreGridRole Role, bool IsActive);
