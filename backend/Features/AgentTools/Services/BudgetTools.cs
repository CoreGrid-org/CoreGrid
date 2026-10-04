using CoreGrid.Api.Data;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Shared.Finance;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed class BudgetTools(CoreGridDbContext db) : IBudgetTools
{
    private const string ReplacementEstimateUnavailable =
        "Replacement value estimation data source is not yet available in the database schema.";

    private const string BudgetTrackingUnavailable =
        "Department budget allocation and committed/spent tracking tables do not exist in the database schema.";

    public async Task<IReadOnlyList<AssetFinancialsDto>> GetFinancialsAsync(AssetSelection selection, CancellationToken cancellationToken = default)
    {
        var assets = await db.Assets.AsNoTracking()
            .Matching(selection)
            .Select(a => new
            {
                a.Id, a.AssetCode, a.DepartmentId, a.AcquisitionCost, a.AcquisitionDate, a.CumulativeMaintenanceCost,
                UsefulLifeYears = a.AssetType != null ? a.AssetType.UsefulLifeYears : 0
            })
            .ToListAsync(cancellationToken);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets.Select(a =>
        {
            var schedule = a.UsefulLifeYears > 0
                ? StraightLineDepreciation.ComputeSchedule(a.AcquisitionCost, a.AcquisitionDate, a.UsefulLifeYears, asOf)
                : null;
            return new AssetFinancialsDto
            {
                AssetId = a.Id,
                AssetCode = a.AssetCode,
                DepartmentId = a.DepartmentId,
                AcquisitionCost = a.AcquisitionCost,
                AcquisitionDate = a.AcquisitionDate,
                UsefulLifeYears = a.UsefulLifeYears,
                AccumulatedDepreciation = schedule?.AccumulatedDepreciation ?? 0m,
                ResidualBookValue = schedule?.CurrentValue ?? a.AcquisitionCost,
                CumulativeMaintenanceCost = a.CumulativeMaintenanceCost,
                ReplacementEstimateNote = ReplacementEstimateUnavailable
            };
        }).ToList();
    }

    public async Task<DepartmentBudgetSummaryDto?> GetDepartmentBudgetSummaryAsync(
        Guid organizationId, Guid departmentId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        var department = await db.Departments.AsNoTracking()
            .Where(d => d.Id == departmentId && d.OrganizationId == organizationId)
            .Select(d => new { d.Id, d.Code, d.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return department is null ? null : new DepartmentBudgetSummaryDto
        {
            DepartmentId = department.Id,
            DepartmentCode = department.Code,
            DepartmentName = department.Name,
            FiscalYear = fiscalYear,
            Status = DepartmentBudgetSummaryDto.NotConfigured,
            Note = BudgetTrackingUnavailable
        };
    }

    public ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request)
    {
        var acquisitionCost = request.AcquisitionCost!.Value;
        var acquisitionDate = request.AcquisitionDate!.Value;
        var usefulLifeYears = request.UsefulLifeYears!.Value;
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = StraightLineDepreciation.ComputeSchedule(acquisitionCost, acquisitionDate, usefulLifeYears, asOf);

        return new ComputeDepreciationResponse
        {
            AcquisitionCost = acquisitionCost,
            AcquisitionDate = acquisitionDate,
            UsefulLifeYears = usefulLifeYears,
            AsOfDate = asOf,
            AnnualDepreciation = schedule.AnnualDepreciation,
            AccumulatedDepreciation = schedule.AccumulatedDepreciation,
            CurrentValue = schedule.CurrentValue
        };
    }
}
