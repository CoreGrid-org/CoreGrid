using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Dashboard;

// FR-081/FR-086: real, organisation-scoped indicators (previously the React
// Admin Dashboard used mock data only — see doc/PROGRESS.md).
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : CoreGridControllerBase
{
    private readonly CoreGridDbContext _db;

    public DashboardController(CoreGridDbContext db) : base(db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var organizationId = currentUser.OrganizationId;

        var totalAssets = await _db.Assets.CountAsync(a => a.OrganizationId == organizationId, cancellationToken);

        var activeAssets = await _db.Assets.CountAsync(
            a => a.OrganizationId == organizationId && a.Status == "ACTIVE", cancellationToken);

        var underMaintenance = await _db.Assets.CountAsync(
            a => a.OrganizationId == organizationId && a.Status == "UNDER_MAINTENANCE", cancellationToken);

        var pendingTransfers = await _db.AssetTransfers.CountAsync(
            t => t.OrganizationId == organizationId &&
                (t.Status == TransferStatus.REQUESTED
                    || t.Status == TransferStatus.APPROVED
                    || t.Status == TransferStatus.IN_TRANSIT),
            cancellationToken);

        var pendingDisposals = await _db.DisposalRequests.CountAsync(
            d => d.OrganizationId == organizationId &&
                (d.Status == DisposalStatus.PENDING
                    || d.Status == DisposalStatus.APPROVED
                    || d.Status == DisposalStatus.REVISION_REQUESTED),
            cancellationToken);

        var openDiscrepancies = await _db.Discrepancies.CountAsync(
            d => d.OrganizationId == organizationId && d.Status == DiscrepancyStatus.Open, cancellationToken);

        var transfersAwaitingApproval = await _db.AssetTransfers.CountAsync(
            t => t.OrganizationId == organizationId && t.Status == TransferStatus.REQUESTED, cancellationToken);

        var disposalsAwaitingApproval = await _db.DisposalRequests.CountAsync(
            d => d.OrganizationId == organizationId && d.Status == DisposalStatus.PENDING, cancellationToken);

        return Ok(new DashboardSummary(
            totalAssets,
            activeAssets,
            underMaintenance,
            pendingTransfers,
            pendingDisposals,
            openDiscrepancies,
            transfersAwaitingApproval + disposalsAwaitingApproval));
    }
}
