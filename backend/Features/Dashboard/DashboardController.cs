using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Dashboard;

// FR-081/FR-082/FR-086: organisation- and department-scoped indicators and
// visualisations (previously the React Admin Dashboard used mock data only
// for the charts — see doc/PROGRESS.md).
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : CoreGridControllerBase
{
    private static readonly string[] ConditionOrder = ["NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE"];

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
        var scope = DashboardScope.Resolve(currentUser);

        var assets = _db.Assets.AsNoTracking().Where(a => a.OrganizationId == organizationId);
        if (scope.IsRestricted) assets = assets.Where(a => a.DepartmentId == scope.DepartmentId);

        var totalAssets = await assets.CountAsync(cancellationToken);
        var activeAssets = await assets.CountAsync(a => a.Status == "ACTIVE", cancellationToken);
        var underMaintenance = await assets.CountAsync(a => a.Status == "UNDER_MAINTENANCE", cancellationToken);

        var transfers = _db.AssetTransfers.AsNoTracking().Where(t => t.OrganizationId == organizationId);
        if (scope.IsRestricted) transfers = transfers.Where(t => t.Asset!.DepartmentId == scope.DepartmentId);

        var pendingTransfers = await transfers.CountAsync(
            t => t.Status == TransferStatus.REQUESTED
                || t.Status == TransferStatus.APPROVED
                || t.Status == TransferStatus.IN_TRANSIT,
            cancellationToken);
        var transfersAwaitingApproval = await transfers.CountAsync(t => t.Status == TransferStatus.REQUESTED, cancellationToken);

        var disposals = _db.DisposalRequests.AsNoTracking().Where(d => d.OrganizationId == organizationId);
        if (scope.IsRestricted) disposals = disposals.Where(d => d.Asset!.DepartmentId == scope.DepartmentId);

        var pendingDisposals = await disposals.CountAsync(
            d => d.Status == DisposalStatus.PENDING
                || d.Status == DisposalStatus.APPROVED
                || d.Status == DisposalStatus.REVISION_REQUESTED,
            cancellationToken);
        var disposalsAwaitingApproval = await disposals.CountAsync(d => d.Status == DisposalStatus.PENDING, cancellationToken);

        var discrepancies = _db.Discrepancies.AsNoTracking().Where(d => d.OrganizationId == organizationId);
        if (scope.IsRestricted) discrepancies = discrepancies.Where(d => d.Asset!.DepartmentId == scope.DepartmentId);

        var openDiscrepancies = await discrepancies.CountAsync(d => d.Status == DiscrepancyStatus.Open, cancellationToken);

        return Ok(new DashboardSummary(
            totalAssets,
            activeAssets,
            underMaintenance,
            pendingTransfers,
            pendingDisposals,
            openDiscrepancies,
            transfersAwaitingApproval + disposalsAwaitingApproval));
    }

    // FR-082: assets by department, assets by condition, maintenance cost by
    // month. Administrator/Auditor only — the two SRS grants these charts
    // to; both are also the org-wide roles under FR-086, so no department
    // restriction ever actually narrows what this endpoint returns, but the
    // scope is still resolved and applied for the same reason GetSummary
    // does: correctness shouldn't depend on who happens to call it today.
    [HttpGet("charts")]
    [Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<DashboardCharts>> GetCharts(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var organizationId = currentUser.OrganizationId;
        var scope = DashboardScope.Resolve(currentUser);
        return await GetChartsCore(organizationId, scope, cancellationToken);
    }

    private async Task<ActionResult<DashboardCharts>> GetChartsCore(Guid organizationId, DepartmentScope scope, CancellationToken cancellationToken)
    {

        var assets = _db.Assets.AsNoTracking().Where(a => a.OrganizationId == organizationId);
        if (scope.IsRestricted) assets = assets.Where(a => a.DepartmentId == scope.DepartmentId);

        // GroupBy → Select-into-record → OrderByDescending doesn't translate
        // (EF tries to re-derive .Value through the record's constructor for
        // the ORDER BY and fails) — materialize into an anonymous type first,
        // same as conditionCounts below, then order and build the record
        // client-side.
        var departmentCounts = await assets
            .GroupBy(a => a.Department!.Name)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var byDepartment = departmentCounts
            .OrderByDescending(d => d.Count)
            .Select(d => new ChartDatum(d.Department, d.Count))
            .ToList();

        var conditionCounts = await assets
            .GroupBy(a => a.Condition)
            .Select(g => new { Condition = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var byCondition = ConditionOrder
            .Select(c => new ChartDatum(c, conditionCounts.FirstOrDefault(x => x.Condition == c)?.Count ?? 0))
            .ToList();

        var maintenance = _db.MaintenanceRecords.AsNoTracking().Where(
            m => m.OrganizationId == organizationId
                && m.Status == MaintenanceStatus.COMPLETED
                && m.CompletionDate != null
                && m.ActualCost != null);
        if (scope.IsRestricted) maintenance = maintenance.Where(m => m.Asset!.DepartmentId == scope.DepartmentId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        var completions = await maintenance
            .Where(m => m.CompletionDate!.Value >= windowStart)
            .Select(m => new { m.CompletionDate, m.ActualCost })
            .ToListAsync(cancellationToken);

        var costByMonthKey = completions
            .GroupBy(r => new DateOnly(r.CompletionDate!.Value.Year, r.CompletionDate.Value.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.ActualCost!.Value));

        var maintenanceCostByMonth = new List<MaintenanceCostDatum>();
        for (var month = windowStart; month <= new DateOnly(today.Year, today.Month, 1); month = month.AddMonths(1))
        {
            var total = costByMonthKey.TryGetValue(month, out var sum) ? sum : 0m;
            maintenanceCostByMonth.Add(new MaintenanceCostDatum(month.ToString("MMM"), total));
        }

        return Ok(new DashboardCharts(byDepartment, byCondition, maintenanceCostByMonth));
    }
}
