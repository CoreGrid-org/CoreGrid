using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public class OrganizationPolicyService : IOrganizationPolicyService
{
    private readonly CoreGridDbContext _context;

    public OrganizationPolicyService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrganizationPolicyDto>> GetPoliciesAsync(Guid organizationId)
    {
        var policies = await _context.OrganizationPolicies
            .AsNoTracking()
            .Include(p => p.AssetType)
            .Where(p => p.OrganizationId == organizationId)
            .OrderBy(p => p.AssetType != null ? p.AssetType.Name : string.Empty)
            .ToListAsync();

        return policies.Select(ToDto).ToList();
    }

    public async Task<OrganizationPolicyDto?> GetPolicyByIdAsync(Guid organizationId, Guid id)
    {
        var policy = await _context.OrganizationPolicies
            .AsNoTracking()
            .Include(p => p.AssetType)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        return policy is null ? null : ToDto(policy);
    }

    public async Task<OrganizationPolicyDto> CreatePolicyAsync(
        Guid organizationId,
        Guid? userId,
        SaveOrganizationPolicyRequest request)
    {
        await ValidateAsync(organizationId, request, existingPolicyId: null);

        var now = DateTimeOffset.UtcNow;

        var policy = new OrganizationPolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetTypeId = request.AssetTypeId,
            RepairToReplaceCostThreshold = request.RepairToReplaceCostThreshold,
            MinimumServiceLifeYears = request.MinimumServiceLifeYears,
            MaxAcceptableFailureFrequency = request.MaxAcceptableFailureFrequency,
            ValuationValidityWindowDays = request.ValuationValidityWindowDays,
            ConfidenceFloor = request.ConfidenceFloor,
            CostVarianceTolerancePercent = request.CostVarianceTolerancePercent,
            OutstandingTransferDays = request.OutstandingTransferDays,
            ApprovalOverduePeriodHours = request.ApprovalOverduePeriodHours,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.OrganizationPolicies.Add(policy);
        await _context.SaveChangesAsync();

        return await GetPolicyByIdAsync(organizationId, policy.Id)
            ?? throw new InvalidOperationException("Policy could not be reloaded after creation.");
    }

    public async Task<OrganizationPolicyDto?> UpdatePolicyAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        SaveOrganizationPolicyRequest request)
    {
        var policy = await _context.OrganizationPolicies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (policy is null)
        {
            return null;
        }

        await ValidateAsync(organizationId, request, existingPolicyId: id);

        policy.AssetTypeId = request.AssetTypeId;
        policy.RepairToReplaceCostThreshold = request.RepairToReplaceCostThreshold;
        policy.MinimumServiceLifeYears = request.MinimumServiceLifeYears;
        policy.MaxAcceptableFailureFrequency = request.MaxAcceptableFailureFrequency;
        policy.ValuationValidityWindowDays = request.ValuationValidityWindowDays;
        policy.ConfidenceFloor = request.ConfidenceFloor;
        policy.CostVarianceTolerancePercent = request.CostVarianceTolerancePercent;
        policy.OutstandingTransferDays = request.OutstandingTransferDays;
        policy.ApprovalOverduePeriodHours = request.ApprovalOverduePeriodHours;
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        policy.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetPolicyByIdAsync(organizationId, policy.Id);
    }

    private async Task ValidateAsync(
        Guid organizationId,
        SaveOrganizationPolicyRequest request,
        Guid? existingPolicyId)
    {
        if (request.AssetTypeId.HasValue)
        {
            var assetTypeExists = await _context.AssetTypes
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.AssetTypeId.Value && t.OrganizationId == organizationId);

            if (!assetTypeExists)
            {
                throw new InvalidOperationException("Asset type was not found.");
            }
        }

        // The unique index on (OrganizationId, AssetTypeId) doesn't catch two
        // org-wide default policies — Postgres treats each NULL AssetTypeId
        // as distinct — so the "at most one policy per asset type, including
        // the org-wide default" rule has to be enforced here instead.
        var duplicateExists = await _context.OrganizationPolicies
            .AsNoTracking()
            .AnyAsync(p =>
                p.OrganizationId == organizationId &&
                p.AssetTypeId == request.AssetTypeId &&
                p.Id != existingPolicyId);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                request.AssetTypeId.HasValue
                    ? "A policy already exists for this asset type."
                    : "An organisation-wide default policy already exists.");
        }
    }

    private static OrganizationPolicyDto ToDto(OrganizationPolicy p) => new()
    {
        Id = p.Id,
        AssetTypeId = p.AssetTypeId,
        AssetTypeName = p.AssetType != null ? p.AssetType.Name : null,
        RepairToReplaceCostThreshold = p.RepairToReplaceCostThreshold,
        MinimumServiceLifeYears = p.MinimumServiceLifeYears,
        MaxAcceptableFailureFrequency = p.MaxAcceptableFailureFrequency,
        ValuationValidityWindowDays = p.ValuationValidityWindowDays,
        ConfidenceFloor = p.ConfidenceFloor,
        CostVarianceTolerancePercent = p.CostVarianceTolerancePercent,
        OutstandingTransferDays = p.OutstandingTransferDays,
        ApprovalOverduePeriodHours = p.ApprovalOverduePeriodHours
    };
}
