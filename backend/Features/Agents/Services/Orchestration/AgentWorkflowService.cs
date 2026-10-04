using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Agents.Services.Orchestration;

public sealed class AgentWorkflowService(CoreGridDbContext db, WorkflowPipeline pipeline) : IAgentWorkflowService
{
    private const int MinimumReasonLength = 10;
    private const string InvalidTransition = "invalid_status_transition";

    public async Task<PagedResult<AgentWorkflowDto>> GetWorkflowsAsync(
        Guid organizationId, AgentWorkflowQueryParameters query, CancellationToken cancellationToken)
    {
        var workflows = WithDisplay(db.AgentWorkflows.AsNoTracking()).Where(w => w.OrganizationId == organizationId);
        if (Enum.TryParse<WorkflowStatus>(query.Status, ignoreCase: true, out var status))
        {
            workflows = workflows.Where(w => w.Status == status);
        }

        var totalCount = await workflows.CountAsync(cancellationToken);
        var page = query.ClampedPage;
        var pageSize = query.ClampedPageSize();
        var items = await workflows
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AgentWorkflowDto>
        {
            Items = items.Select(WorkflowMapper.ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await WithDisplay(db.AgentWorkflows.AsNoTracking())
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        return workflow is null ? null : WorkflowMapper.ToDto(workflow);
    }

    public async Task<WorkflowExecutionSummaryDto?> GetExecutionSummaryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await WithDisplay(db.AgentWorkflows.AsNoTracking())
            .Include(w => w.Steps)
            .Include(w => w.Approvals).ThenInclude(a => a.DecidedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        return workflow is null ? null : WorkflowMapper.ToExecutionSummary(workflow);
    }

    public async Task<AgentWorkflowDto> CreateWorkflowAsync(
        Guid organizationId, Guid userId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(organizationId, request, cancellationToken);
        await EnsureNotAlreadyRunningAsync(scope, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetTypeId = scope.AssetTypeId,
            AssetId = scope.AssetId,
            Objective = request.Objective.Trim(),
            Status = WorkflowStatus.PLANNING,
            ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            CorrelationId = Guid.NewGuid().ToString("N"),
            InitiatedByUserId = userId,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        await pipeline.RunAsync(workflow, scope, excludedAction: null, cancellationToken);

        return await GetWorkflowByIdAsync(organizationId, workflow.Id, cancellationToken)
            ?? throw new InvalidOperationException("Workflow could not be reloaded after creation.");
    }

    public async Task<AgentWorkflowDto?> ResumeAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await LoadAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        EnsureResumable(workflow, $"Workflow is {workflow.Status} — only an in-progress evaluation can be resumed.");
        await pipeline.RunAsync(workflow, EvaluationScope.Of(workflow), excludedAction: null, cancellationToken);
        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public async Task<AgentWorkflowDto?> RerunNodeAsync(Guid organizationId, Guid id, string agent, CancellationToken cancellationToken)
    {
        var workflow = await LoadAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        EnsureResumable(workflow,
            $"Workflow is {workflow.Status} — the {ExecutionTrace.Label(agent)} Agent only runs while an evaluation is in progress.");
        await pipeline.RerunNodeAsync(workflow, agent, cancellationToken);
        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public async Task<AgentWorkflowDto?> EvaluatePolicyAsync(
        Guid organizationId, Guid id, EvaluatePolicyRequest request, CancellationToken cancellationToken)
    {
        var workflow = await LoadAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new ConflictException(
                $"Workflow is {workflow.Status} — policy evaluation only runs from PLANNING or ANALYZING.", InvalidTransition);
        }

        await pipeline.EvaluateManualProposalAsync(workflow, request, cancellationToken);
        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public async Task<AgentWorkflowDto?> DecideAsync(
        Guid organizationId, Guid id, Guid deciderUserId, DecideWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await LoadAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        ValidateDecision(workflow, request);

        var now = DateTimeOffset.UtcNow;
        db.AgentApprovals.Add(new AgentApproval
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            Decision = request.Decision,
            DecidedByUserId = deciderUserId,
            Reason = request.Reason.Trim(),
            WorkflowSnapshot = WorkflowMapper.Write(WorkflowMapper.ToDto(workflow)),
            DecidedAt = now
        });

        var excludedAction = WorkflowRouting.ApplyDecision(workflow, request.Decision, now);
        await db.SaveChangesAsync(cancellationToken);

        if (workflow.Status == WorkflowStatus.ANALYZING)
        {
            await pipeline.RunAsync(workflow, EvaluationScope.Of(workflow), excludedAction, cancellationToken);
        }

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    private static void ValidateDecision(AgentWorkflow workflow, DecideWorkflowRequest request)
    {
        if (workflow.Status != WorkflowStatus.AWAITING_APPROVAL)
        {
            throw new ConflictException("This workflow is not awaiting approval.", InvalidTransition);
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < MinimumReasonLength)
        {
            throw new ValidationException(nameof(request.Reason), "A decision reason of at least 10 characters is required.");
        }

        if (request.Decision is not (WorkflowDecisions.Approve or WorkflowDecisions.Reject or WorkflowDecisions.Revise))
        {
            throw new ValidationException(nameof(request.Decision), "Decision must be APPROVE, REJECT or REVISE.");
        }
    }

    private static void EnsureResumable(AgentWorkflow workflow, string message)
    {
        if (!WorkflowRouting.ResumableStatuses.Contains(workflow.Status))
        {
            throw new ConflictException(message, InvalidTransition);
        }
    }

    private async Task EnsureNotAlreadyRunningAsync(EvaluationScope scope, CancellationToken cancellationToken)
    {
        var alreadyRunning = await db.AgentWorkflows.AsNoTracking().AnyAsync(
            w => w.OrganizationId == scope.OrganizationId
                && w.AssetTypeId == scope.AssetTypeId
                && w.AssetId == scope.AssetId
                && WorkflowRouting.InFlightStatuses.Contains(w.Status),
            cancellationToken);

        if (alreadyRunning)
        {
            throw new ConflictException($"An evaluation is already running for {scope.Label}.", "workflow_already_running");
        }
    }

    private async Task<EvaluationScope> ResolveScopeAsync(
        Guid organizationId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken)
    {
        var assetTypeId = request.AssetTypeId;
        string? assetCode = null;

        if (request.AssetId is { } assetId)
        {
            var asset = await db.Assets.AsNoTracking()
                .Where(a => a.Id == assetId && a.OrganizationId == organizationId)
                .Select(a => new { a.AssetTypeId, a.AssetCode, a.Status })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationException(nameof(request.AssetId), "Asset not found.");

            if (assetTypeId.HasValue && assetTypeId != asset.AssetTypeId)
            {
                throw new ValidationException(nameof(request.AssetId), "The asset doesn't belong to the selected asset type.");
            }

            if (asset.Status == AssetStatuses.Disposed)
            {
                throw new BusinessRuleException("This asset is disposed — no further evaluation is possible.", "asset_disposed");
            }

            assetTypeId = asset.AssetTypeId;
            assetCode = asset.AssetCode;
        }

        if (assetTypeId is null)
        {
            throw new ValidationException(nameof(request.AssetTypeId), "Choose an asset type to evaluate.");
        }

        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == assetTypeId && t.OrganizationId == organizationId)
            .Select(t => new { t.Id, t.Name, Category = t.AssetCategory!.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException(nameof(request.AssetTypeId), "Asset type not found.");

        return new EvaluationScope(organizationId, type.Id, type.Name, type.Category, request.AssetId, assetCode);
    }

    private Task<AgentWorkflow?> LoadAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        WithDisplay(db.AgentWorkflows).FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

    private static IQueryable<AgentWorkflow> WithDisplay(IQueryable<AgentWorkflow> workflows) => workflows
        .Include(w => w.AssetType).ThenInclude(t => t!.AssetCategory)
        .Include(w => w.Asset)
        .Include(w => w.InitiatedByUser);
}
