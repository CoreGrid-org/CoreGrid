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
// Manages asset lifecycle decision workflows.
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
    private readonly IBudgetAgentClient _budgetAgent;

    public AgentWorkflowService(
        CoreGridDbContext db,
        IAgentToolsService agentTools,
        IPolicyRuleEngine ruleEngine,
        IPlannerAgentClient plannerAgent,
        IMaintenanceAnalysisToolsService maintenanceAnalysisTools,
        IBudgetAgentClient budgetAgent)
    {
        _db = db;
        _agentTools = agentTools;
        _ruleEngine = ruleEngine;
        _plannerAgent = plannerAgent;
        _maintenanceAnalysisTools = maintenanceAnalysisTools;
        _budgetAgent = budgetAgent;
    }

    public async Task<PagedResult<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, AgentWorkflowQueryParameters query, CancellationToken cancellationToken)
    {
        var workflows = _db.AgentWorkflows.AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .Where(w => w.OrganizationId == organizationId);

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
        var workflow = await _db.AgentWorkflows.AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);

        return workflow is null ? null : MapToDto(workflow);
    }

    public async Task<WorkflowExecutionSummaryDto?> GetExecutionSummaryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _db.AgentWorkflows.AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.InitiatedByUser)
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

    public async Task<AgentWorkflowDto> CreateWorkflowAsync(
        Guid organizationId,
        Guid userId,
        CreateAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var assetId = request.AssetId!.Value;

        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken)
            ?? throw new ValidationException(nameof(request.AssetId), "Asset not found.");

        // FR-068: refuse a terminal asset or an evaluation already running for it.
        if (asset.Status == AssetStatuses.Disposed)
        {
            throw new BusinessRuleException("This asset is disposed — no further evaluation is possible.", "asset_disposed");
        }

        var alreadyRunning = await _db.AgentWorkflows.AsNoTracking().AnyAsync(
            w => w.AssetId == assetId && w.OrganizationId == organizationId && InFlightStatuses.Contains(w.Status.ToString()),
            cancellationToken);
        if (alreadyRunning)
        {
            throw new ConflictException("An evaluation is already running for this asset.", "workflow_already_running");
        }

        var now = DateTimeOffset.UtcNow;
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = assetId,
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
                Agent = AgentNames.Planner,
                Sequence = 1,
                OutputSummary = plan.InScope
                    ? $"Plan created with {plan.Steps.Count} steps."
                    : $"Rejected: {plan.RejectionReason}",
                DurationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds),
                Status = "SUCCESS",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            // Node 2 (Maintenance Analysis, SRS §7.3) & Node 3 (Budget Analysis, SRS §7.3):
            // run automatically right after Planner accepts the objective.
            // In-process, assembling facts and lifecycle option assessments
            // for Policy Compliance (node 4) to evaluate. Failures are recorded
            // as advisory execution steps and swallowed rather than failing
            // the whole workflow.
            if (plan.InScope)
            {
                await RunMaintenanceAnalysisAsync(workflow, cancellationToken);
                await RunBudgetAnalysisAsync(workflow, cancellationToken);
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

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.MaintenanceAnalysisSucceeded(
                workflow.Id, stats, (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds), DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Advisory-only node — record the failure, never fail the workflow over it.
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.MaintenanceAnalysisFailed(
                workflow.Id, ex.Message, (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds), DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RunBudgetAnalysisAsync(AgentWorkflow workflow, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var failureStats = !string.IsNullOrEmpty(workflow.MaintenanceAnalysis)
                ? JsonSerializer.Deserialize<AgentToolsDtos.FailureStatisticsDto>(workflow.MaintenanceAnalysis)
                : null;

            if (failureStats is null)
            {
                failureStats = new AgentToolsDtos.FailureStatisticsDto
                {
                    AssetId = workflow.AssetId,
                    AssetCode = workflow.Asset?.AssetCode ?? string.Empty,
                    EvaluatedAsOf = DateOnly.FromDateTime(DateTime.UtcNow)
                };
            }

            Guid? departmentId = workflow.Asset?.DepartmentId;
            if (!departmentId.HasValue)
            {
                departmentId = await _db.Assets.AsNoTracking()
                    .Where(a => a.Id == workflow.AssetId)
                    .Select(a => (Guid?)a.DepartmentId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var assessment = await _budgetAgent.RunAssessmentAsync(
                workflow.OrganizationId,
                workflow.AssetId,
                departmentId,
                DateTime.UtcNow.Year,
                failureStats,
                cancellationToken);

            workflow.BudgetAnalysis = JsonSerializer.Serialize(assessment);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            _db.AgentExecutionSteps.Add(AgentExecutionSteps.BudgetAnalysisSucceeded(
                workflow.Id, assessment, (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds), DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Advisory-only node — record the failure, never fail the whole workflow over it.
            _db.AgentExecutionSteps.Add(AgentExecutionSteps.BudgetAnalysisFailed(
                workflow.Id, ex.Message, (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds), DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AgentWorkflowDto?> RunBudgetAnalysisAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _db.AgentWorkflows
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new ConflictException(
                $"Workflow is {workflow.Status} — the Budget Analysis Agent only runs from PLANNING or ANALYZING.",
                "invalid_status_transition");
        }

        await RunBudgetAnalysisAsync(workflow, cancellationToken);

        return await GetWorkflowByIdAsync(organizationId, id, cancellationToken);
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
            throw new ConflictException($"Workflow is {workflow.Status} — policy evaluation only runs from PLANNING or ANALYZING.", "invalid_status_transition");
        }

        var complianceState = await _agentTools.GetAssetComplianceStateAsync(organizationId, workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Could not load the asset's compliance state.");

        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Asset not found.");

        var policy = await _agentTools.GetOrganizationPoliciesAsync(organizationId, asset.AssetTypeId, cancellationToken)
            ?? throw new BusinessRuleException("No organisation policy is configured — cannot evaluate compliance.", "no_policy_configured");

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

        // FR-027 (B12): lifecycle history when a workflow records a
        // recommendation — covers both this manual /evaluate submission and
        // PolicyComplianceAgentService's agent-driven call, since both funnel
        // through this one method.
        _db.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = workflow.AssetId,
            ActorUserId = null,
            EventType = AssetHistoryEventTypes.AgentRecommendation,
            Description = $"Workflow {workflow.Id} recorded recommendation '{request.ProposedRecommendation}' (verdict {validation.Verdict}).",
            PreviousValue = null,
            NewValue = JsonSerializer.Serialize(new { recommendation = request.ProposedRecommendation, verdict = validation.Verdict, isHighImpact = validation.IsHighImpact }),
            CreatedAt = now
        });

        _db.AgentExecutionSteps.Add(new AgentExecutionStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            Agent = AgentNames.PolicyCompliance,
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

        switch (request.Decision)
        {
            case WorkflowDecisions.Approve:
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
        BudgetAnalysis = string.IsNullOrEmpty(w.BudgetAnalysis)
            ? null
            : JsonSerializer.Deserialize<FinancialAssessmentResultDto>(w.BudgetAnalysis),
        CorrelationId = w.CorrelationId,
        InitiatedByUserId = w.InitiatedByUserId,
        InitiatedByEmail = w.InitiatedByUser?.Email,
        StartedAt = w.StartedAt,
        CompletedAt = w.CompletedAt,
        CreatedAt = w.CreatedAt
    };
}
