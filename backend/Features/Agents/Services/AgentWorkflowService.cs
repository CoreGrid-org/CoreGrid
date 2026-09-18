using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.Services;
using AgentToolsDtos = CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Agents.Services;

// SRS §7: owns workflow initiation (FR-067/068), the Policy Compliance
// node + deterministic gate (§7.6) combined into EvaluatePolicyAsync since
// nodes 1-3 (Planner/Maintenance/Budget) don't exist yet to feed it
// automatically, and the human-approval checkpoint (§7.7, AI-13 to AI-20).
public class AgentWorkflowService : IAgentWorkflowService
{
    private static readonly string[] InFlightStatuses =
    [
        nameof(WorkflowStatus.PLANNING), nameof(WorkflowStatus.ANALYZING),
        nameof(WorkflowStatus.VALIDATING), nameof(WorkflowStatus.AWAITING_APPROVAL)
    ];

    private readonly CoreGridDbContext _db;
    private readonly IAgentToolsService _agentTools;
    private readonly IPolicyRuleEngine _ruleEngine;
    private readonly IPlannerAgentClient _plannerAgent;
    private readonly IMaintenanceAnalysisToolsService _maintenanceAnalysisTools;

    public AgentWorkflowService(
        CoreGridDbContext db,
        IAgentToolsService agentTools,
        IPolicyRuleEngine ruleEngine,
        IPlannerAgentClient plannerAgent,
        IMaintenanceAnalysisToolsService maintenanceAnalysisTools)
    {
        _db = db;
        _agentTools = agentTools;
        _ruleEngine = ruleEngine;
        _plannerAgent = plannerAgent;
        _maintenanceAnalysisTools = maintenanceAnalysisTools;
    }

    public async Task<List<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, string? status, CancellationToken cancellationToken)
    {
        var query = _db.AgentWorkflows.AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .Where(w => w.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkflowStatus>(status, true, out var statusFilter))
        {
            query = query.Where(w => w.Status == statusFilter);
        }

        var workflows = await query.OrderByDescending(w => w.CreatedAt).ToListAsync(cancellationToken);
        return workflows.Select(MapToDto).ToList();
    }

    public async Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _db.AgentWorkflows.AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        return workflow is null ? null : MapToDto(workflow);
    }

    public async Task<AgentWorkflowDto> CreateWorkflowAsync(
        Guid organizationId,
        Guid userId,
        CreateAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Asset not found.");

        // FR-068: refuse a terminal asset or an evaluation already running for it.
        if (asset.Status == "DISPOSED")
        {
            throw new InvalidOperationException("This asset is disposed — no further evaluation is possible.");
        }

        var alreadyRunning = await _db.AgentWorkflows.AsNoTracking().AnyAsync(
            w => w.AssetId == request.AssetId && w.OrganizationId == organizationId && InFlightStatuses.Contains(w.Status.ToString()),
            cancellationToken);
        if (alreadyRunning)
        {
            throw new InvalidOperationException("An evaluation is already running for this asset.");
        }

        if (string.IsNullOrWhiteSpace(request.Objective))
        {
            throw new InvalidOperationException("An objective is required to initiate an evaluation.");
        }

        var now = DateTimeOffset.UtcNow;
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = request.AssetId,
            Objective = request.Objective.Trim(),
            Status = WorkflowStatus.PLANNING,
            ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            RevisionCount = 0,
            CorrelationId = Guid.NewGuid().ToString("N"),
            InitiatedByUserId = userId,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AgentWorkflows.Add(workflow);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var started = DateTimeOffset.UtcNow;
            var plan = await _plannerAgent.CreatePlanAsync(
                workflow.AssetId,
                workflow.Objective,
                workflow.InitiatedByUserId,
                workflow.OrganizationId,
                cancellationToken);

            workflow.Plan = JsonSerializer.Serialize(plan);
            workflow.Status = plan.InScope ? WorkflowStatus.ANALYZING : WorkflowStatus.FAILED_SAFE;
            workflow.FailureReason = plan.InScope ? null : plan.RejectionReason;
            workflow.CompletedAt = plan.InScope ? null : DateTimeOffset.UtcNow;
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            _db.AgentExecutionSteps.Add(new AgentExecutionStep
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                Agent = "Planner",
                Sequence = 1,
                OutputSummary = plan.InScope
                    ? $"Plan created with {plan.Steps.Count} steps."
                    : $"Rejected: {plan.RejectionReason}",
                DurationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds),
                Status = "SUCCESS",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            // Node 2 (Maintenance Analysis, SRS §7.3): runs automatically
            // right after Planner accepts the objective, same as node 4
            // eventually will once node 3 exists too. In-process, no model
            // call — it only assembles facts for nodes 3/4 (or a human
            // reviewer) to read, so a failure here is recorded and
            // swallowed rather than failing the whole workflow the way a
            // Planner failure does; it never gates anything.
            if (plan.InScope)
            {
                await RunMaintenanceAnalysisAsync(workflow, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            workflow.Status = WorkflowStatus.FAILED_SAFE;
            workflow.FailureReason = "Planner Agent failed: " + ex.Message;
            workflow.CompletedAt = DateTimeOffset.UtcNow;
            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await GetWorkflowByIdAsync(organizationId, workflow.Id, cancellationToken)
            ?? throw new InvalidOperationException("Workflow could not be reloaded after creation.");
    }

    private async Task RunMaintenanceAnalysisAsync(AgentWorkflow workflow, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var stats = await _maintenanceAnalysisTools.ComputeFailureStatisticsAsync(
                workflow.OrganizationId, workflow.AssetId, cancellationToken);

            if (stats is null)
            {
                return;
            }

            workflow.MaintenanceAnalysis = JsonSerializer.Serialize(stats);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            _db.AgentExecutionSteps.Add(new AgentExecutionStep
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                Agent = "MaintenanceAnalysis",
                Sequence = 2,
                OutputSummary = $"RepairCount={stats.RepairCount}, CostTrend={stats.CostTrend}, "
                    + $"Projected12moCost={stats.ProjectedNextTwelveMonthsCost}",
                DurationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds),
                Status = "SUCCESS",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Advisory-only node — record the failure, never fail the workflow over it.
            _db.AgentExecutionSteps.Add(new AgentExecutionStep
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                Agent = "MaintenanceAnalysis",
                Sequence = 2,
                OutputSummary = "Maintenance analysis failed.",
                DurationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds),
                Status = "FAILED",
                Error = ex.Message,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AgentWorkflowDto?> EvaluatePolicyAsync(
        Guid organizationId,
        Guid id,
        EvaluatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _db.AgentWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new InvalidOperationException($"Workflow is {workflow.Status} — policy evaluation only runs from PLANNING or ANALYZING.");
        }

        var complianceState = await _agentTools.GetAssetComplianceStateAsync(organizationId, workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Could not load the asset's compliance state.");

        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Asset not found.");

        var policy = await _agentTools.GetOrganizationPoliciesAsync(organizationId, asset.AssetTypeId, cancellationToken)
            ?? throw new InvalidOperationException("No organisation policy is configured — cannot evaluate compliance.");

        var facts = new DTOs.PolicyEvaluationFacts
        {
            ProposedRecommendation = request.ProposedRecommendation,
            AssetCondition = complianceState.CurrentCondition,
            AssetStatus = complianceState.CurrentStatus,
            ElapsedServiceLifeYears = complianceState.ElapsedServiceLifeYears,
            HasValuation = complianceState.HasValuation,
            ValuationDate = complianceState.ValuationDate,
            OpenMaintenanceCount = complianceState.OpenMaintenanceCount,
            OpenTransferCount = complianceState.OpenTransferCount,
            RepairToReplaceRatio = request.FinancialAssessment?.RepairToReplaceRatio,
            ProjectedRepairCost = request.FinancialAssessment?.ProjectedRepairCost,
            BudgetHeadroom = request.FinancialAssessment?.BudgetHeadroom,
            Confidence = request.FinancialAssessment?.Confidence,
            MinimumServiceLifeYears = policy.MinimumServiceLifeYears,
            ValuationValidityWindowDays = policy.ValuationValidityWindowDays,
            RepairToReplaceCostThreshold = policy.RepairToReplaceCostThreshold,
            ConfidenceFloor = policy.ConfidenceFloor,
            EvaluatedAsOf = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var validation = _ruleEngine.Evaluate(facts);
        var now = DateTimeOffset.UtcNow;

        workflow.ValidationResult = JsonSerializer.Serialize(validation);
        workflow.Recommendation = request.ProposedRecommendation;
        workflow.IsHighImpact = validation.IsHighImpact;
        workflow.UpdatedAt = now;

        _db.AgentExecutionSteps.Add(new AgentExecutionStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            Agent = "PolicyCompliance",
            Sequence = 4,
            OutputSummary = $"Verdict={validation.Verdict}, IsHighImpact={validation.IsHighImpact}",
            Status = "SUCCESS",
            CreatedAt = now
        });

        // Stage 2 of the deterministic gate (§7.6): FAIL → safe failure;
        // PASS + high-impact → interrupt; PASS + low-impact → advisory;
        // NEEDS_REVISION → back to analysis, capped at 2 revisions (AI-20).
        switch (validation.Verdict)
        {
            case "FAIL":
                workflow.Status = WorkflowStatus.FAILED_SAFE;
                workflow.FailureReason = string.Join(" ", validation.BlockingReasons);
                workflow.CompletedAt = now;
                break;

            case "NEEDS_REVISION":
                if (workflow.RevisionCount >= 2)
                {
                    workflow.Status = WorkflowStatus.REVISION_REQUESTED;
                    workflow.CompletedAt = now;
                }
                else
                {
                    workflow.RevisionCount++;
                    workflow.Status = WorkflowStatus.ANALYZING;
                }
                break;

            default: // PASS
                if (validation.IsHighImpact)
                {
                    workflow.Status = WorkflowStatus.AWAITING_APPROVAL;
                    workflow.ApprovalStatus = ApprovalStatus.PENDING;
                }
                else
                {
                    workflow.Status = WorkflowStatus.COMPLETED_ADVISORY;
                    workflow.ApprovalStatus = ApprovalStatus.NOT_REQUIRED;
                    workflow.CompletedAt = now;
                }
                break;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public async Task<AgentWorkflowDto?> DecideAsync(
        Guid organizationId,
        Guid id,
        Guid deciderUserId,
        DecideWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _db.AgentWorkflows
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        // AI-13/AI-18: only a paused, awaiting-approval workflow can be
        // decided; no other state permits a decision, so no business state
        // can ever change while a workflow is merely paused.
        if (workflow.Status != WorkflowStatus.AWAITING_APPROVAL)
        {
            throw new InvalidOperationException("This workflow is not awaiting approval.");
        }

        // AI-16: a decision reason of at least 10 characters.
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
        {
            throw new InvalidOperationException("A decision reason of at least 10 characters is required.");
        }

        if (request.Decision is not ("APPROVE" or "REJECT" or "REVISE"))
        {
            throw new InvalidOperationException("Decision must be APPROVE, REJECT or REVISE.");
        }

        var now = DateTimeOffset.UtcNow;

        _db.AgentApprovals.Add(new AgentApproval
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            Decision = request.Decision,
            DecidedByUserId = deciderUserId,
            Reason = request.Reason.Trim(),
            WorkflowSnapshot = JsonSerializer.Serialize(MapToDto(workflow)),
            DecidedAt = now
        });

        switch (request.Decision)
        {
            case "APPROVE":
                // AI-17: on approval, the API — not the agent service — would
                // execute the authorised action through the ordinary business
                // service (Component A/B/C's own guarded endpoints). That
                // cross-component execution wiring isn't built yet — the
                // decision is recorded and the workflow marked APPROVED, but
                // no business record is touched by this call. Same posture
                // as Component C's P6 precondition: stubbed pending the rest
                // of the agent subsystem, not silently faked.
                workflow.Status = WorkflowStatus.APPROVED;
                workflow.ApprovalStatus = ApprovalStatus.APPROVED;
                workflow.CompletedAt = now;
                break;

            case "REJECT":
                workflow.Status = WorkflowStatus.REJECTED;
                workflow.ApprovalStatus = ApprovalStatus.REJECTED;
                workflow.CompletedAt = now;
                break;

            default: // REVISE
                if (workflow.RevisionCount >= 2)
                {
                    workflow.Status = WorkflowStatus.REVISION_REQUESTED;
                    workflow.CompletedAt = now;
                }
                else
                {
                    workflow.RevisionCount++;
                    workflow.Status = WorkflowStatus.ANALYZING;
                    workflow.ApprovalStatus = ApprovalStatus.NOT_REQUIRED;
                }
                break;
        }

        workflow.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    private static AgentWorkflowDto MapToDto(AgentWorkflow w) => new()
    {
        Id = w.Id,
        AssetId = w.AssetId,
        AssetCode = w.Asset?.AssetCode ?? string.Empty,
        Objective = w.Objective,
        Status = w.Status.ToString(),
        Recommendation = w.Recommendation,
        IsHighImpact = w.IsHighImpact,
        ApprovalStatus = w.ApprovalStatus.ToString(),
        RevisionCount = w.RevisionCount,
        FailureReason = w.FailureReason,
        Plan = string.IsNullOrEmpty(w.Plan)
            ? null
            : JsonSerializer.Deserialize<PlannerExecutionPlan>(w.Plan),
        ValidationResult = string.IsNullOrEmpty(w.ValidationResult)
            ? null
            : JsonSerializer.Deserialize<DTOs.PolicyValidation>(w.ValidationResult),
        MaintenanceAnalysis = string.IsNullOrEmpty(w.MaintenanceAnalysis)
            ? null
            : JsonSerializer.Deserialize<AgentToolsDtos.FailureStatisticsDto>(w.MaintenanceAnalysis),
        CorrelationId = w.CorrelationId,
        InitiatedByUserId = w.InitiatedByUserId,
        InitiatedByEmail = w.InitiatedByUser?.Email,
        StartedAt = w.StartedAt,
        CompletedAt = w.CompletedAt,
        CreatedAt = w.CreatedAt
    };
}
