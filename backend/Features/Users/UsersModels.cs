using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Users;

public record CreateUserRequest(string Email, string GivenName, string FamilyName, string Password, CoreGridRole Role);

public record UpdateUserRequest(CoreGridRole Role, Guid? DepartmentId);

public record UserResponse(
    Guid Id,
    string Email,
    string GivenName,
    string FamilyName,
    CoreGridRole Role,
    Guid? DepartmentId,
    bool IsActive,
    DateTimeOffset CreatedAt);
