using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Data.Auditing;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private (Guid? UserId, Guid? OrganizationId)? _cached;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public async Task<(Guid? UserId, Guid? OrganizationId)> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_cached.HasValue)
        {
            return _cached.Value;
        }

        var externalSubjectId =
            _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(externalSubjectId))
        {
            _cached = (null, null);
            return _cached.Value;
        }

        // A fresh scope/DbContext, not the ambient CoreGridDbContext — this
        // is called from inside that context's own SavingChangesAsync
        // interceptor, and EF Core does not allow a second operation to
        // start on a DbContext instance while one is already in flight.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreGridDbContext>();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.ExternalSubjectId == externalSubjectId)
            .Select(u => new { u.Id, u.OrganizationId })
            .FirstOrDefaultAsync(cancellationToken);

        _cached = user is null ? (null, null) : (user.Id, user.OrganizationId);
        return _cached.Value;
    }
}
