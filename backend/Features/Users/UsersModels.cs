using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Users;

// Defines the request and response models for user management.
public record CreateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string GivenName,
    [Required, MaxLength(100)] string FamilyName,
    [Required, MinLength(8), MaxLength(200)] string Password,
    [Required] CoreGridRole? Role);

// An Administrator sets a new password for another user (or themselves) in
// ThunderID; the old password stops working immediately.
public record ResetPasswordRequest([Required, MinLength(8), MaxLength(200)] string NewPassword);

public record UpdateUserRequest([Required] CoreGridRole? Role, Guid? DepartmentId);

public record UserResponse(
    Guid Id,
    string Email,
    string GivenName,
    string FamilyName,
    CoreGridRole Role,
    Guid? DepartmentId,
    bool IsActive,
    DateTimeOffset CreatedAt);
