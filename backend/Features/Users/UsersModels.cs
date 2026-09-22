using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Users;

// Defines the request and response models for user management.
public record CreateUserRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(100)] string GivenName,
    [property: Required, MaxLength(100)] string FamilyName,
    [property: Required, MinLength(8), MaxLength(200)] string Password,
    [property: Required] CoreGridRole? Role);

public record UpdateUserRequest([property: Required] CoreGridRole? Role, Guid? DepartmentId);

public record UserResponse(
    Guid Id,
    string Email,
    string GivenName,
    string FamilyName,
    CoreGridRole Role,
    Guid? DepartmentId,
    bool IsActive,
    DateTimeOffset CreatedAt);
