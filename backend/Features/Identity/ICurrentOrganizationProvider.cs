namespace CoreGrid.Api.Features.Identity;

// Provides the organization ID for the current request.
public interface ICurrentOrganizationProvider
{
    Guid? OrganizationId { get; }
}
