using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public class OrganizationPolicyService : IOrganizationPolicyService
{
    private static readonly Expression<Func<OrganizationPolicy, OrganizationPolicyDto>> ToDtoExpression = p => new OrganizationPolicyDto
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

    private static readonly Func<OrganizationPolicy, OrganizationPolicyDto> ToDto = ToDtoExpression.Compile();

    private readonly CoreGridDbContext _context;

    public OrganizationPolicyService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<OrganizationPolicyDto>> GetPoliciesAsync(Guid organizationId, PagedQuery query, CancellationToken cancellationToken)
    {
        var policies = _context.OrganizationPolicies
            .AsNoTracking()
            .Include(p => p.AssetType)
            .Where(p => p.OrganizationId == organizationId)
            .OrderBy(p => p.AssetType != null ? p.AssetType.Name : string.Empty);

        return await policies.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);
    }

    public async Task<OrganizationPolicyDto?> GetPolicyByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var policy = await _context.OrganizationPolicies
            .AsNoTracking()
            .Include(p => p.AssetType)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);

        return policy is null ? null : ToDto(policy);
    }

    public async Task<OrganizationPolicyDto> CreatePolicyAsync(
        Guid organizationId,
        Guid? userId,
        SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(organizationId, request, existingPolicyId: null, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // [Required][Range] on the DTO makes a missing/out-of-range value
        // 400 for a model-bound HTTP caller before this method ever runs.
        var policy = new OrganizationPolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetTypeId = request.AssetTypeId,
            RepairToReplaceCostThreshold = request.RepairToReplaceCostThreshold!.Value,
            MinimumServiceLifeYears = request.MinimumServiceLifeYears!.Value,
            MaxAcceptableFailureFrequency = request.MaxAcceptableFailureFrequency!.Value,
            ValuationValidityWindowDays = request.ValuationValidityWindowDays!.Value,
            ConfidenceFloor = request.ConfidenceFloor!.Value,
            CostVarianceTolerancePercent = request.CostVarianceTolerancePercent!.Value,
            OutstandingTransferDays = request.OutstandingTransferDays!.Value,
            ApprovalOverduePeriodHours = request.ApprovalOverduePeriodHours!.Value,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.OrganizationPolicies.Add(policy);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPolicyByIdAsync(organizationId, policy.Id, cancellationToken)
            ?? throw new InvalidOperationException("Policy could not be reloaded after creation.");
    }

    public async Task<OrganizationPolicyDto?> UpdatePolicyAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await _context.OrganizationPolicies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);

        if (policy is null)
        {
            return null;
        }

        await ValidateAsync(organizationId, request, existingPolicyId: id, cancellationToken);

        policy.AssetTypeId = request.AssetTypeId;
        policy.RepairToReplaceCostThreshold = request.RepairToReplaceCostThreshold!.Value;
        policy.MinimumServiceLifeYears = request.MinimumServiceLifeYears!.Value;
        policy.MaxAcceptableFailureFrequency = request.MaxAcceptableFailureFrequency!.Value;
        policy.ValuationValidityWindowDays = request.ValuationValidityWindowDays!.Value;
        policy.ConfidenceFloor = request.ConfidenceFloor!.Value;
        policy.CostVarianceTolerancePercent = request.CostVarianceTolerancePercent!.Value;
        policy.OutstandingTransferDays = request.OutstandingTransferDays!.Value;
        policy.ApprovalOverduePeriodHours = request.ApprovalOverduePeriodHours!.Value;
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        policy.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPolicyByIdAsync(organizationId, policy.Id, cancellationToken);
    }

    private async Task ValidateAsync(
        Guid organizationId,
        SaveOrganizationPolicyRequest request,
        Guid? existingPolicyId,
        CancellationToken cancellationToken)
    {
        if (request.AssetTypeId.HasValue)
        {
            var assetTypeExists = await _context.AssetTypes
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.AssetTypeId.Value && t.OrganizationId == organizationId, cancellationToken);

            if (!assetTypeExists)
            {
                throw new ValidationException(nameof(request.AssetTypeId), "Asset type was not found.");
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
                p.Id != existingPolicyId,
                cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                request.AssetTypeId.HasValue
                    ? "A policy already exists for this asset type."
                    : "An organisation-wide default policy already exists.",
                "duplicate_policy");
        }
    }
}
