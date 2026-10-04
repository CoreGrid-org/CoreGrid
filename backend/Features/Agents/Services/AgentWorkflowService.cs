using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.Services;
using AgentToolsDtos = CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Agents.Services;

// The orchestrator (SRS §7.2.1): resolves the evaluation scope once, asks the
// Planner for a plan, then executes that plan node by node — Maintenance
// Analysis → Budget Analysis → Policy Compliance → Deterministic Gate — in a
// single request, so a workflow comes back already routed. It never calls a
// business tool itself; each node reads only through its own allow-list.
public class AgentWorkflowService : IAgentWorkflowService
{
    private static readonly WorkflowStatus[] InFlightStatuses =
    [
        WorkflowStatus.PLANNING, WorkflowStatus.ANALYZING, WorkflowStatus.VALIDATING, WorkflowStatus.AWAITING_APPROVAL
    ];

    private static readonly WorkflowStatus[] ResumableStatuses =
    [
        WorkflowStatus.PLANNING, WorkflowStatus.ANALYZING, WorkflowStatus.VALIDATING
    ];

    private readonly CoreGridDbContext _db;
    private readonly IPlannerAgentClient _plannerAgent;
    private readonly IMaintenanceAnalysisToolsService _maintenanceAnalysisTools;
    private readonly IBudgetAgentClient _budgetAgent;
    private readonly IPolicyComplianceEvaluator _policyEvaluator;

    public AgentWorkflowService(
        CoreGridDbContext db,
        IPlannerAgentClient plannerAgent,
        IMaintenanceAnalysisToolsService maintenanceAnalysisTools,
        IBudgetAgentClient budgetAgent,
        IPolicyComplianceEvaluator policyEvaluator)
    {
        _db = db;
        _plannerAgent = plannerAgent;
        _maintenanceAnalysisTools = maintenanceAnalysisTools;
        _budgetAgent = budgetAgent;
        _policyEvaluator = policyEvaluator;
    }

    // ── Reads ────────────────────────────────────────────────────────────────

    private IQueryable<AgentWorkflow> WorkflowsWithDisplay() => _db.AgentWorkflows.AsNoTracking()
        .Include(w => w.AssetType).ThenInclude(t => t!.AssetCategory)
        .Include(w => w.Asset)
        .Include(w => w.InitiatedByUser);

    public async Task<PagedResult<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, AgentWorkflowQueryParameters query, CancellationToken cancellationToken)
    {
        var workflows = WorkflowsWithDisplay().Where(w => w.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<WorkflowStatus>(query.Status, true, out var statusFilter))
        {
            workflows = workflows.Where(w => w.Status == statusFilter);
        }

        var totalCount = await workflows.CountAsync(cancellationToken);
        var page = query.ClampedPage;
        var pageSize = query.ClampedPageSize();

        var pageItems = await workflows
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AgentWorkflowDto>
        {
            Items = pageItems.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await WorkflowsWithDisplay()
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        return workflow is null ? null : MapToDto(workflow);
    }

    public async Task<WorkflowExecutionSummaryDto?> GetExecutionSummaryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await WorkflowsWithDisplay()
            .Include(w => w.Steps)
            .Include(w => w.Approvals).ThenInclude(a => a.DecidedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        if (workflow is null)
        {
            return null;
        }

        return new WorkflowExecutionSummaryDto
        {
            Workflow = MapToDto(workflow),
            Steps = workflow.Steps
                .OrderBy(s => s.Sequence)
                .ThenBy(s => s.CreatedAt)
                .Select(s => new AgentExecutionStepDto
                {
                    Id = s.Id,
                    Agent = s.Agent,
                    Sequence = s.Sequence,
                    InputHash = s.InputHash,
                    OutputSummary = s.OutputSummary,
                    DurationMs = s.DurationMs,
                    Status = s.Status,
                    Error = s.Error,
                    CreatedAt = s.CreatedAt
                })
                .ToList(),
            Approvals = workflow.Approvals
                .OrderBy(a => a.DecidedAt)
                .Select(a => new AgentApprovalDto
                {
                    Id = a.Id,
                    Decision = a.Decision,
                    DecidedByUserId = a.DecidedByUserId,
                    DecidedByEmail = a.DecidedByUser?.Email,
                    Reason = a.Reason,
                    DecidedAt = a.DecidedAt
                })
                .ToList()
        };
    }

    // ── Create & run ─────────────────────────────────────────────────────────

    public async Task<AgentWorkflowDto> CreateWorkflowAsync(
        Guid organizationId,
        Guid userId,
        CreateAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveRequestedScopeAsync(organizationId, request, cancellationToken);

        // FR-068: one in-flight evaluation per target (type, or type + asset).
        var alreadyRunning = await _db.AgentWorkflows.AsNoTracking().AnyAsync(
            w => w.OrganizationId == organizationId
                && w.AssetTypeId == scope.AssetTypeId
                && w.AssetId == scope.AssetId
                && InFlightStatuses.Contains(w.Status),
            cancellationToken);
        if (alreadyRunning)
        {
            throw new ConflictException($"An evaluation is already running for {scope.Label}.", "workflow_already_running");
        }

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
            RevisionCount = 0,
            CorrelationId = Guid.NewGuid().ToString("N"),
            InitiatedByUserId = userId,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AgentWorkflows.Add(workflow);
        await _db.SaveChangesAsync(cancellationToken);

        await RunPipelineAsync(workflow, scope, excludedAction: null, cancellationToken);

        return await GetWorkflowByIdAsync(organizationId, workflow.Id, cancellationToken)
            ?? throw new InvalidOperationException("Workflow could not be reloaded after creation.");
    }

    public async Task<AgentWorkflowDto?> ResumeAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await LoadForUpdateAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        if (!ResumableStatuses.Contains(workflow.Status))
        {
            throw new ConflictException($"Workflow is {workflow.Status} — only an in-progress evaluation can be resumed.", "invalid_status_transition");
        }

        await RunPipelineAsync(workflow, ScopeOf(workflow), excludedAction: null, cancellationToken);
        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public async Task<AgentWorkflowDto?> RerunNodeAsync(Guid organizationId, Guid id, string agent, CancellationToken cancellationToken)
    {
        var workflow = await LoadForUpdateAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        if (!ResumableStatuses.Contains(workflow.Status))
        {
            throw new ConflictException(
                $"Workflow is {workflow.Status} — the {AgentExecutionSteps.Label(agent)} Agent only runs while an evaluation is in progress.",
                "invalid_status_transition");
        }

        var context = new PipelineContext(ScopeOf(workflow), ExcludedAction: null);
        switch (agent)
        {
            case AgentNames.MaintenanceAnalysis:
                await RunMaintenanceNodeAsync(workflow, context, SequenceOf(workflow, agent), cancellationToken);
                break;
            case AgentNames.BudgetAnalysis:
                await RunBudgetNodeAsync(workflow, context, SequenceOf(workflow, agent), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(agent), agent, "Only analysis nodes can be re-run on their own.");
        }

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    public Task<AgentWorkflowDto?> RunBudgetAnalysisAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        RerunNodeAsync(organizationId, id, AgentNames.BudgetAnalysis, cancellationToken);

    private sealed record PipelineContext(EvaluationScope Scope, string? ExcludedAction)
    {
        public IReadOnlyList<AgentToolsDtos.FailureStatisticsDto>? Maintenance { get; set; }
        public FinancialAssessmentResultDto? Budget { get; set; }
        public PolicyValidation? Validation { get; set; }
    }

    // Planner (if no plan yet), then the plan's steps in order. Every node
    // persists its own output and trace row, so an interruption leaves an
    // honest partial trace and the workflow can be resumed.
    private async Task RunPipelineAsync(AgentWorkflow workflow, EvaluationScope scope, string? excludedAction, CancellationToken cancellationToken)
    {
        var plan = string.IsNullOrEmpty(workflow.Plan)
            ? await RunPlannerAsync(workflow, scope, cancellationToken)
            : JsonSerializer.Deserialize<PlannerExecutionPlan>(workflow.Plan);
        if (plan is null || !plan.InScope) return;

        workflow.Status = WorkflowStatus.ANALYZING;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;

        var context = new PipelineContext(scope, excludedAction);
        foreach (var step in plan.Steps.OrderBy(s => s.Seq))
        {
            var sequence = step.Seq + 1; // the Planner is step 1
            switch (step.Agent)
            {
                case AgentNames.MaintenanceAnalysis:
                    await RunMaintenanceNodeAsync(workflow, context, sequence, cancellationToken);
                    break;
                case AgentNames.BudgetAnalysis:
                    await RunBudgetNodeAsync(workflow, context, sequence, cancellationToken);
                    break;
                case AgentNames.PolicyCompliance:
                    if (!await RunPolicyNodeAsync(workflow, context, sequence, cancellationToken)) return;
                    break;
                case AgentNames.DeterministicGate:
                    await RunGateNodeAsync(workflow, context.Validation, sequence, automated: true, cancellationToken);
                    break;
            }
        }
    }

    private async Task<PlannerExecutionPlan?> RunPlannerAsync(AgentWorkflow workflow, EvaluationScope scope, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var plan = await _plannerAgent.CreatePlanAsync(scope, workflow.Objective, workflow.InitiatedByUserId, cancellationToken);

            workflow.Plan = JsonSerializer.Serialize(plan);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            if (!plan.InScope)
            {
                workflow.Status = WorkflowStatus.FAILED_SAFE;
                workflow.FailureReason = plan.RejectionReason;
                workflow.CompletedAt = DateTimeOffset.UtcNow;
            }

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
                workflow.Id, AgentNames.Planner, 1, AgentExecutionSteps.PlanSummary(plan), AgentExecutionSteps.Elapsed(started)));
            await _db.SaveChangesAsync(cancellationToken);
            return plan;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Failed(workflow.Id, AgentNames.Planner, 1, ex.Message, AgentExecutionSteps.Elapsed(started)));
            FailSafe(workflow, "Planner Agent failed: " + ex.Message);
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }
    }

    // Node 2 — deterministic; facts only, never a recommendation.
    private async Task RunMaintenanceNodeAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            context.Maintenance = await _maintenanceAnalysisTools.ComputeFleetFailureStatisticsAsync(
                workflow.OrganizationId, workflow.AssetTypeId, workflow.AssetId, cancellationToken);

            var aggregate = MaintenanceAggregation.Aggregate(context.Scope, context.Maintenance);
            workflow.MaintenanceAnalysis = JsonSerializer.Serialize(aggregate);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
                workflow.Id, AgentNames.MaintenanceAnalysis, sequence, AgentExecutionSteps.MaintenanceSummary(aggregate), AgentExecutionSteps.Elapsed(started)));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Advisory node — record the failure; downstream nodes degrade to "no repair history".
            context.Maintenance = [];
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Failed(
                workflow.Id, AgentNames.MaintenanceAnalysis, sequence, ex.Message, AgentExecutionSteps.Elapsed(started)));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Node 3 — deterministic triage, optionally model-ranked.
    private async Task RunBudgetNodeAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            // Re-run or resume without node 2 in this request: recompute its
            // per-asset input (milliseconds, deterministic) rather than trust a summary.
            context.Maintenance ??= await _maintenanceAnalysisTools.ComputeFleetFailureStatisticsAsync(
                workflow.OrganizationId, workflow.AssetTypeId, workflow.AssetId, cancellationToken);

            context.Budget = await _budgetAgent.RunAssessmentAsync(context.Scope, context.Maintenance, cancellationToken);
            workflow.BudgetAnalysis = JsonSerializer.Serialize(context.Budget);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
                workflow.Id, AgentNames.BudgetAnalysis, sequence, AgentExecutionSteps.BudgetSummary(context.Budget), AgentExecutionSteps.Elapsed(started)));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Advisory node — Policy Compliance falls back to condition-based proposals.
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Failed(
                workflow.Id, AgentNames.BudgetAnalysis, sequence, ex.Message, AgentExecutionSteps.Elapsed(started)));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Node 4 — deterministic; returns false when the workflow had to fail safe.
    private async Task<bool> RunPolicyNodeAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken)
    {
        workflow.Status = WorkflowStatus.VALIDATING;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;

        var started = DateTimeOffset.UtcNow;
        try
        {
            context.Budget ??= Deserialize<FinancialAssessmentResultDto>(workflow.BudgetAnalysis);

            var outcome = await _policyEvaluator.EvaluateAsync(
                context.Scope, context.Maintenance, context.Budget, new PolicyEvaluationOptions(ExcludedAction: context.ExcludedAction), cancellationToken);

            RecordOutcome(workflow, outcome);
            context.Validation = outcome.Validation;

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
                workflow.Id, AgentNames.PolicyCompliance, sequence, outcome.Summary, AgentExecutionSteps.Elapsed(started)));
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is BusinessRuleException or InvalidOperationException)
        {
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Failed(
                workflow.Id, AgentNames.PolicyCompliance, sequence, ex.Message, AgentExecutionSteps.Elapsed(started)));
            FailSafe(workflow, "Policy Compliance Agent failed: " + ex.Message);
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task RunGateNodeAsync(AgentWorkflow workflow, PolicyValidation? validation, int sequence, bool automated, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        validation ??= Deserialize<PolicyValidation>(workflow.ValidationResult);
        if (validation is null)
        {
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Failed(
                workflow.Id, AgentNames.DeterministicGate, sequence, "No policy validation to gate.", AgentExecutionSteps.Elapsed(started)));
            FailSafe(workflow, "No policy validation was produced — nothing can be routed.");
        }
        else
        {
            var summary = ApplyGate(workflow, validation, automated);
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
                workflow.Id, AgentNames.DeterministicGate, sequence, summary, AgentExecutionSteps.Elapsed(started)));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Stage 2 of the deterministic gate (§7.6): FAIL → safe failure;
    // PASS + high-impact → interrupt for approval; PASS + low-impact →
    // advisory. NEEDS_REVISION: in the automated pipeline every permitted
    // candidate has already been tried, so it ends as REVISION_REQUESTED; a
    // human's manual proposal goes back to analysis, capped at 2 (AI-20).
    private static string ApplyGate(AgentWorkflow workflow, PolicyValidation validation, bool automated)
    {
        var now = DateTimeOffset.UtcNow;
        workflow.UpdatedAt = now;

        switch (validation.Verdict)
        {
            case "FAIL":
                workflow.Status = WorkflowStatus.FAILED_SAFE;
                workflow.FailureReason = string.Join(" ", validation.BlockingReasons);
                workflow.CompletedAt = now;
                return "Policy FAIL → safe failure. No business record was changed.";

            case "NEEDS_REVISION" when automated || workflow.RevisionCount >= 2:
                workflow.Status = WorkflowStatus.REVISION_REQUESTED;
                workflow.FailureReason = string.Join(" ", validation.BlockingReasons);
                workflow.CompletedAt = now;
                return "No policy-permitted action yet → revision requested. " + string.Join(" ", validation.BlockingReasons);

            case "NEEDS_REVISION":
                workflow.RevisionCount++;
                workflow.Status = WorkflowStatus.ANALYZING;
                return $"Needs revision → back to analysis (revision {workflow.RevisionCount} of 2).";

            default: // PASS
                if (validation.IsHighImpact)
                {
                    workflow.Status = WorkflowStatus.AWAITING_APPROVAL;
                    workflow.ApprovalStatus = ApprovalStatus.PENDING;
                    return "High-impact recommendation → paused for Administrator approval.";
                }

                workflow.Status = WorkflowStatus.COMPLETED_ADVISORY;
                workflow.ApprovalStatus = ApprovalStatus.NOT_REQUIRED;
                workflow.FailureReason = null;
                workflow.CompletedAt = now;
                return "Low-impact and policy-compliant → completed as advisory.";
        }
    }

    // Persists node 4's result and the FR-027 lifecycle history entries.
    private void RecordOutcome(AgentWorkflow workflow, PolicyOutcome outcome)
    {
        var now = DateTimeOffset.UtcNow;
        workflow.ValidationResult = JsonSerializer.Serialize(outcome.Validation);
        workflow.AgentOutputs = JsonSerializer.Serialize(outcome.Fleet);
        workflow.Recommendation = outcome.Recommendation;
        workflow.IsHighImpact = outcome.Validation.IsHighImpact;
        workflow.UpdatedAt = now;

        // Single asset: always recorded. Fleet: only assets that actually got
        // a permitted, non-trivial action — RETAIN across a fleet is noise.
        var recorded = workflow.AssetId.HasValue
            ? outcome.Fleet.Assets
            : outcome.Fleet.Assets.Where(a => a.Verdict == "PASS" && a.Action != "RETAIN");

        foreach (var asset in recorded)
        {
            _db.AssetHistoryEntries.Add(new AssetHistory
            {
                Id = Guid.NewGuid(),
                OrganizationId = workflow.OrganizationId,
                AssetId = asset.AssetId,
                ActorUserId = null,
                EventType = AssetHistoryEventTypes.AgentRecommendation,
                Description = $"Workflow {workflow.Id} recorded recommendation '{asset.Action}' (verdict {asset.Verdict}).",
                PreviousValue = null,
                NewValue = JsonSerializer.Serialize(new { recommendation = asset.Action, verdict = asset.Verdict, isHighImpact = asset.IsHighImpact }),
                CreatedAt = now
            });
        }
    }

    // ── Manual evaluation & human decision ───────────────────────────────────

    public async Task<AgentWorkflowDto?> EvaluatePolicyAsync(
        Guid organizationId,
        Guid id,
        EvaluatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await LoadForUpdateAsync(organizationId, id, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new ConflictException($"Workflow is {workflow.Status} — policy evaluation only runs from PLANNING or ANALYZING.", "invalid_status_transition");
        }

        var started = DateTimeOffset.UtcNow;
        var outcome = await _policyEvaluator.EvaluateAsync(
            ScopeOf(workflow),
            maintenanceByAsset: null,
            Deserialize<FinancialAssessmentResultDto>(workflow.BudgetAnalysis),
            new PolicyEvaluationOptions(ForcedAction: request.ProposedRecommendation, ManualFacts: request.FinancialAssessment),
            cancellationToken);

        RecordOutcome(workflow, outcome);
        _db.AgentExecutionSteps.Add(AgentExecutionSteps.Succeeded(
            workflow.Id, AgentNames.PolicyCompliance, 4, "Manual proposal · " + outcome.Summary, AgentExecutionSteps.Elapsed(started)));

        await RunGateNodeAsync(workflow, outcome.Validation, 5, automated: false, cancellationToken);
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
            .Include(w => w.AssetType).ThenInclude(t => t!.AssetCategory)
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        // AI-13/AI-18: only a paused, awaiting-approval workflow can be
        // decided; no other state permits a decision, so no business state
        // can ever change while a workflow is merely paused.
        if (workflow.Status != WorkflowStatus.AWAITING_APPROVAL)
        {
            throw new ConflictException("This workflow is not awaiting approval.", "invalid_status_transition");
        }

        // AI-16: a decision reason of at least 10 characters.
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
        {
            throw new ValidationException(nameof(request.Reason), "A decision reason of at least 10 characters is required.");
        }

        if (request.Decision is not (WorkflowDecisions.Approve or WorkflowDecisions.Reject or WorkflowDecisions.Revise))
        {
            throw new ValidationException(nameof(request.Decision), "Decision must be APPROVE, REJECT or REVISE.");
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

        var rerunExcluding = (string?)null;
        switch (request.Decision)
        {
            case WorkflowDecisions.Approve:
                // AI-17: on approval, the API — not the agent service — would
                // execute the authorised action through the ordinary business
                // service (Component A/B/C's own guarded endpoints). That
                // cross-component execution wiring isn't built yet — the
                // decision is recorded and the workflow marked APPROVED, but
                // no business record is touched by this call.
                workflow.Status = WorkflowStatus.APPROVED;
                workflow.ApprovalStatus = ApprovalStatus.APPROVED;
                workflow.CompletedAt = now;
                break;

            case WorkflowDecisions.Reject:
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
                    rerunExcluding = workflow.Recommendation;
                }
                break;
        }

        workflow.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        // A revision re-runs the analysis straight away, never re-proposing
        // the action the reviewer just sent back.
        if (workflow.Status == WorkflowStatus.ANALYZING)
        {
            await RunPipelineAsync(workflow, ScopeOf(workflow), rerunExcluding, cancellationToken);
        }

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EvaluationScope> ResolveRequestedScopeAsync(Guid organizationId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken)
    {
        Guid? assetTypeId = request.AssetTypeId;
        string? assetCode = null;

        if (request.AssetId is { } assetId)
        {
            var asset = await _db.Assets.AsNoTracking()
                .Where(a => a.Id == assetId && a.OrganizationId == organizationId)
                .Select(a => new { a.AssetTypeId, a.AssetCode, a.Status })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationException(nameof(request.AssetId), "Asset not found.");

            if (assetTypeId.HasValue && assetTypeId != asset.AssetTypeId)
            {
                throw new ValidationException(nameof(request.AssetId), "The asset doesn't belong to the selected asset type.");
            }

            // FR-068: refuse a terminal asset.
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

        var type = await _db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == assetTypeId && t.OrganizationId == organizationId)
            .Select(t => new { t.Id, t.Name, Category = t.AssetCategory!.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException(nameof(request.AssetTypeId), "Asset type not found.");

        return new EvaluationScope(organizationId, type.Id, type.Name, type.Category, request.AssetId, assetCode);
    }

    private Task<AgentWorkflow?> LoadForUpdateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        _db.AgentWorkflows
            .Include(w => w.AssetType).ThenInclude(t => t!.AssetCategory)
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

    private static EvaluationScope ScopeOf(AgentWorkflow workflow) => new(
        workflow.OrganizationId,
        workflow.AssetTypeId,
        workflow.AssetType?.Name ?? "Asset type",
        workflow.AssetType?.AssetCategory?.Name ?? string.Empty,
        workflow.AssetId,
        workflow.Asset?.AssetCode);

    // A node's trace sequence: its position in the stored plan, after the Planner.
    private static int SequenceOf(AgentWorkflow workflow, string agent)
    {
        var plan = Deserialize<PlannerExecutionPlan>(workflow.Plan);
        var step = plan?.Steps.FirstOrDefault(s => s.Agent == agent);
        return step is null ? Array.FindIndex(AgentRegistry.Agents, a => a.Name == agent) + 2 : step.Seq + 1;
    }

    private static void FailSafe(AgentWorkflow workflow, string reason)
    {
        workflow.Status = WorkflowStatus.FAILED_SAFE;
        workflow.FailureReason = reason;
        workflow.CompletedAt = DateTimeOffset.UtcNow;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static T? Deserialize<T>(string? json) where T : class =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);

    private static AgentWorkflowDto MapToDto(AgentWorkflow w)
    {
        var budget = Deserialize<FinancialAssessmentResultDto>(w.BudgetAnalysis);
        if (budget is not null) budget.Assets = null; // per-asset rows travel in Fleet instead

        return new AgentWorkflowDto
        {
            Id = w.Id,
            Scope = w.AssetId.HasValue ? "ASSET" : "ASSET_TYPE",
            AssetTypeId = w.AssetTypeId,
            AssetTypeName = w.AssetType?.Name ?? string.Empty,
            CategoryName = w.AssetType?.AssetCategory?.Name ?? string.Empty,
            AssetId = w.AssetId,
            AssetCode = w.Asset?.AssetCode ?? string.Empty,
            Objective = w.Objective,
            Status = w.Status.ToString(),
            Recommendation = w.Recommendation,
            IsHighImpact = w.IsHighImpact,
            ApprovalStatus = w.ApprovalStatus.ToString(),
            RevisionCount = w.RevisionCount,
            FailureReason = w.FailureReason,
            Plan = Deserialize<PlannerExecutionPlan>(w.Plan),
            ValidationResult = Deserialize<PolicyValidation>(w.ValidationResult),
            MaintenanceAnalysis = Deserialize<AgentToolsDtos.FailureStatisticsDto>(w.MaintenanceAnalysis),
            BudgetAnalysis = budget,
            Fleet = Deserialize<FleetEvaluationDto>(w.AgentOutputs),
            CorrelationId = w.CorrelationId,
            InitiatedByUserId = w.InitiatedByUserId,
            InitiatedByEmail = w.InitiatedByUser?.Email,
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt,
            CreatedAt = w.CreatedAt
        };
    }
}
