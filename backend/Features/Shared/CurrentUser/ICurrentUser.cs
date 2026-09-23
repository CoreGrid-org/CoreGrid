using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.CurrentUser;

// Provides the current user's request-scoped context.
public interface ICurrentUser
{
    // Indicates whether the current user context has been resolved.
    bool IsResolved { get; }

    Guid Id { get; }
    Guid OrganizationId { get; }
    CoreGridRole Role { get; }
    Guid? DepartmentId { get; }

    // Indicates whether the caller is the agent service principal.
    bool IsServicePrincipal { get; }
}
