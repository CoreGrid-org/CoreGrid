using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Users;

public record CreateUserRequest(string Email, string GivenName, string FamilyName, string Password, CoreGridRole Role);

public record UserResponse(
    Guid Id,
    string Email,
    string GivenName,
    string FamilyName,
    CoreGridRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
