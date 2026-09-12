using Microsoft.AspNetCore.Http;

namespace CoreGrid.Api.Identity;

public class CurrentOrganizationProvider(IHttpContextAccessor httpContextAccessor) : ICurrentOrganizationProvider
{
    public Guid? OrganizationId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst("organization_id")?.Value;
            return Guid.TryParse(claim, out var organizationId) ? organizationId : null;
        }
    }
}
