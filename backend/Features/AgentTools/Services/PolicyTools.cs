using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed class PolicyTools(CoreGridDbContext db) : IPolicyTools
{
    private const decimal DaysPerYear = 365.25m;

    public async Task<OrganizationPolicyFactsDto?> GetOrganizationPoliciesAsync(
        Guid organizationId, Guid? assetTypeId, CancellationToken cancellationToken = default)
    {
        var policies = db.OrganizationPolicies.AsNoTracking().Where(p => p.OrganizationId == organizationId);
        var policy = await policies.FirstOrDefaultAsync(p => p.AssetTypeId == assetTypeId, cancellationToken)
            ?? await policies.FirstOrDefaultAsync(p => p.AssetTypeId == null, cancellationToken);

        return policy is null ? null : new OrganizationPolicyFactsDto
        {
            AssetTypeId = policy.AssetTypeId,
            RepairToReplaceCostThreshold = policy.RepairToReplaceCostThreshold,
            MinimumServiceLifeYears = policy.MinimumServiceLifeYears,
            MaxAcceptableFailureFrequency = policy.MaxAcceptableFailureFrequency,
            ValuationValidityWindowDays = policy.ValuationValidityWindowDays,
            ConfidenceFloor = policy.ConfidenceFloor
        };
    }

    public async Task<IReadOnlyList<AssetComplianceStateDto>> GetComplianceStateAsync(
        AssetSelection selection, CancellationToken cancellationToken = default)
    {
        var assets = await db.Assets.AsNoTracking()
            .Matching(selection)
            .Select(a => new { a.Id, a.AssetCode, a.Status, a.Condition, a.AcquisitionDate })
            .ToListAsync(cancellationToken);
        if (assets.Count == 0) return [];

        var ids = assets.Select(a => a.Id).ToList();
        var organizationId = selection.OrganizationId;

        var valuations = await db.DisposalRequests.AsNoTracking()
            .Where(d => ids.Contains(d.AssetId) && d.OrganizationId == organizationId && d.ValuationDate != null)
            .GroupBy(d => d.AssetId)
            .Select(g => new { AssetId = g.Key, Latest = g.Max(d => d.ValuationDate) })
            .ToDictionaryAsync(g => g.AssetId, g => g.Latest, cancellationToken);

        var openMaintenance = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => ids.Contains(m.AssetId) && m.OrganizationId == organizationId
                && m.Status != MaintenanceStatus.COMPLETED && m.Status != MaintenanceStatus.CANCELLED)
            .GroupBy(m => m.AssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AssetId, g => g.Count, cancellationToken);

        var openTransfers = await db.AssetTransfers.AsNoTracking()
            .Where(t => ids.Contains(t.AssetId) && t.OrganizationId == organizationId
                && t.Status != TransferStatus.COMPLETED && t.Status != TransferStatus.REJECTED && t.Status != TransferStatus.CANCELLED)
            .GroupBy(t => t.AssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AssetId, g => g.Count, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets.Select(a =>
        {
            var valuationDate = valuations.GetValueOrDefault(a.Id);
            return new AssetComplianceStateDto
            {
                AssetId = a.Id,
                AssetCode = a.AssetCode,
                CurrentStatus = a.Status,
                CurrentCondition = a.Condition,
                IsCondemned = a.Status == AssetStatuses.Condemned,
                HasValuation = valuationDate.HasValue,
                ValuationDate = valuationDate,
                OpenMaintenanceCount = openMaintenance.GetValueOrDefault(a.Id),
                OpenTransferCount = openTransfers.GetValueOrDefault(a.Id),
                ElapsedServiceLifeYears = Math.Round((today.DayNumber - a.AcquisitionDate.DayNumber) / DaysPerYear, 1)
            };
        }).ToList();
    }
}
