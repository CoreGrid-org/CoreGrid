using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services.Budget;
using CoreGrid.Api.Features.Agents.Services.Maintenance;
using CoreGrid.Api.Features.Agents.Services.Planner;
using CoreGrid.Api.Features.Agents.Services.Policy;
using CoreGrid.Api.Features.Shared.Exceptions;

namespace CoreGrid.Api.Features.Agents.Services.Orchestration;

public sealed class WorkflowPipeline(
    CoreGridDbContext db,
    IPlannerAgent planner,
    IMaintenanceTools maintenanceTools,
    IBudgetAgent budgetAgent,
    IPolicyComplianceEvaluator policyEvaluator)
{
    private const int PolicyStepSequence = 4;
    private const int GateStepSequence = 5;

    public async Task RunAsync(AgentWorkflow workflow, EvaluationScope scope, string? excludedAction, CancellationToken cancellationToken)
    {
        var plan = string.IsNullOrEmpty(workflow.Plan)
            ? await RunPlannerAsync(workflow, scope, cancellationToken)
            : WorkflowMapper.Read<PlannerExecutionPlan>(workflow.Plan);
        if (plan is null || !plan.InScope) return;

        workflow.Status = WorkflowStatus.ANALYZING;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;

        var context = new PipelineContext(scope, excludedAction);
        foreach (var step in plan.Steps.OrderBy(s => s.Seq))
        {
            var sequence = step.Seq + 1;
            switch (step.Agent)
            {
                case AgentNames.MaintenanceAnalysis:
                    await RunMaintenanceAsync(workflow, context, sequence, cancellationToken);
                    break;
                case AgentNames.BudgetAnalysis:
                    await RunBudgetAsync(workflow, context, sequence, cancellationToken);
                    break;
                case AgentNames.PolicyCompliance:
                    if (!await RunPolicyAsync(workflow, context, sequence, cancellationToken)) return;
                    break;
                case AgentNames.DeterministicGate:
                    await RunGateAsync(workflow, context.Validation, sequence, automated: true, cancellationToken);
                    break;
            }
        }
    }

    public Task RerunNodeAsync(AgentWorkflow workflow, string agent, CancellationToken cancellationToken)
    {
        var context = new PipelineContext(EvaluationScope.Of(workflow), excludedAction: null);
        var sequence = SequenceOf(workflow, agent);

        return agent switch
        {
            AgentNames.MaintenanceAnalysis => RunMaintenanceAsync(workflow, context, sequence, cancellationToken),
            AgentNames.BudgetAnalysis => RunBudgetAsync(workflow, context, sequence, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Only analysis nodes can be re-run on their own.")
        };
    }

    public async Task EvaluateManualProposalAsync(AgentWorkflow workflow, EvaluatePolicyRequest request, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var outcome = await policyEvaluator.EvaluateAsync(
            EvaluationScope.Of(workflow),
            maintenanceByAsset: null,
            WorkflowMapper.Read<FinancialAssessmentResultDto>(workflow.BudgetAnalysis),
            new PolicyEvaluationOptions(ForcedAction: request.ProposedRecommendation, ManualFacts: request.FinancialAssessment),
            cancellationToken);

        RecordOutcome(workflow, outcome);
        db.AgentExecutionSteps.Add(ExecutionTrace.Success(
            workflow.Id, AgentNames.PolicyCompliance, PolicyStepSequence, "Manual proposal · " + outcome.Summary, started));

        await RunGateAsync(workflow, outcome.Validation, GateStepSequence, automated: false, cancellationToken);
    }

    private async Task<PlannerExecutionPlan?> RunPlannerAsync(AgentWorkflow workflow, EvaluationScope scope, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var plan = await planner.CreatePlanAsync(scope, workflow.Objective, cancellationToken);
            workflow.Plan = WorkflowMapper.Write(plan);
            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            if (!plan.InScope) WorkflowRouting.FailSafe(workflow, plan.RejectionReason);

            db.AgentExecutionSteps.Add(ExecutionTrace.Success(workflow.Id, AgentNames.Planner, 1, ExecutionTrace.PlanSummary(plan), started));
            await db.SaveChangesAsync(cancellationToken);
            return plan;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            db.AgentExecutionSteps.Add(ExecutionTrace.Failure(workflow.Id, AgentNames.Planner, 1, ex.Message, started));
            WorkflowRouting.FailSafe(workflow, "Planner Agent failed: " + ex.Message);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
    }

    private Task RunMaintenanceAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken) =>
        RunAdvisoryNodeAsync(workflow, AgentNames.MaintenanceAnalysis, sequence, async () =>
        {
            context.Maintenance = await maintenanceTools.GetFailureStatisticsAsync(context.Scope.Selection, cancellationToken);
            var aggregate = MaintenanceAggregation.Aggregate(context.Scope, context.Maintenance);
            workflow.MaintenanceAnalysis = WorkflowMapper.Write(aggregate);
            return ExecutionTrace.MaintenanceSummary(aggregate);
        },
        onFailure: () => context.Maintenance = [],
        cancellationToken);

    private Task RunBudgetAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken) =>
        RunAdvisoryNodeAsync(workflow, AgentNames.BudgetAnalysis, sequence, async () =>
        {
            context.Maintenance ??= await maintenanceTools.GetFailureStatisticsAsync(context.Scope.Selection, cancellationToken);
            context.Budget = await budgetAgent.RunAssessmentAsync(context.Scope, context.Maintenance, cancellationToken);
            workflow.BudgetAnalysis = WorkflowMapper.Write(context.Budget);
            return ExecutionTrace.BudgetSummary(context.Budget);
        },
        onFailure: null,
        cancellationToken);

    private async Task RunAdvisoryNodeAsync(
        AgentWorkflow workflow, string agent, int sequence, Func<Task<string>> execute, Action? onFailure, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var summary = await execute();
            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            db.AgentExecutionSteps.Add(ExecutionTrace.Success(workflow.Id, agent, sequence, summary, started));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            onFailure?.Invoke();
            db.AgentExecutionSteps.Add(ExecutionTrace.Failure(workflow.Id, agent, sequence, ex.Message, started));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> RunPolicyAsync(AgentWorkflow workflow, PipelineContext context, int sequence, CancellationToken cancellationToken)
    {
        workflow.Status = WorkflowStatus.VALIDATING;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;

        var started = DateTimeOffset.UtcNow;
        try
        {
            context.Budget ??= WorkflowMapper.Read<FinancialAssessmentResultDto>(workflow.BudgetAnalysis);
            var outcome = await policyEvaluator.EvaluateAsync(
                context.Scope, context.Maintenance, context.Budget,
                new PolicyEvaluationOptions(ExcludedAction: context.ExcludedAction), cancellationToken);

            RecordOutcome(workflow, outcome);
            context.Validation = outcome.Validation;
            db.AgentExecutionSteps.Add(ExecutionTrace.Success(workflow.Id, AgentNames.PolicyCompliance, sequence, outcome.Summary, started));
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is BusinessRuleException or InvalidOperationException)
        {
            db.AgentExecutionSteps.Add(ExecutionTrace.Failure(workflow.Id, AgentNames.PolicyCompliance, sequence, ex.Message, started));
            WorkflowRouting.FailSafe(workflow, "Policy Compliance Agent failed: " + ex.Message);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task RunGateAsync(
        AgentWorkflow workflow, PolicyValidation? validation, int sequence, bool automated, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        validation ??= WorkflowMapper.Read<PolicyValidation>(workflow.ValidationResult);

        if (validation is null)
        {
            db.AgentExecutionSteps.Add(ExecutionTrace.Failure(
                workflow.Id, AgentNames.DeterministicGate, sequence, "No policy validation to gate.", started));
            WorkflowRouting.FailSafe(workflow, "No policy validation was produced — nothing can be routed.");
        }
        else
        {
            var summary = WorkflowRouting.ApplyVerdict(workflow, validation, automated);
            db.AgentExecutionSteps.Add(ExecutionTrace.Success(workflow.Id, AgentNames.DeterministicGate, sequence, summary, started));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void RecordOutcome(AgentWorkflow workflow, PolicyOutcome outcome)
    {
        var now = DateTimeOffset.UtcNow;
        workflow.ValidationResult = WorkflowMapper.Write(outcome.Validation);
        workflow.AgentOutputs = WorkflowMapper.Write(outcome.Fleet);
        workflow.Recommendation = outcome.Recommendation;
        workflow.IsHighImpact = outcome.Validation.IsHighImpact;
        workflow.UpdatedAt = now;

        var recorded = workflow.AssetId.HasValue
            ? outcome.Fleet.Assets
            : outcome.Fleet.Assets.Where(a => a.Verdict == PolicyVerdicts.Pass && a.Action != LifecycleActions.Retain);

        db.AssetHistoryEntries.AddRange(recorded.Select(asset => new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = workflow.OrganizationId,
            AssetId = asset.AssetId,
            EventType = AssetHistoryEventTypes.AgentRecommendation,
            Description = $"Workflow {workflow.Id} recorded recommendation '{asset.Action}' (verdict {asset.Verdict}).",
            NewValue = WorkflowMapper.Write(new { recommendation = asset.Action, verdict = asset.Verdict, isHighImpact = asset.IsHighImpact }),
            CreatedAt = now
        }));
    }

    private static int SequenceOf(AgentWorkflow workflow, string agent)
    {
        var step = WorkflowMapper.Read<PlannerExecutionPlan>(workflow.Plan)?.Steps.FirstOrDefault(s => s.Agent == agent);
        return step is null ? AgentRegistry.PositionOf(agent) + 2 : step.Seq + 1;
    }

    private sealed class PipelineContext(EvaluationScope scope, string? excludedAction)
    {
        public EvaluationScope Scope { get; } = scope;
        public string? ExcludedAction { get; } = excludedAction;
        public IReadOnlyList<FailureStatisticsDto>? Maintenance { get; set; }
        public FinancialAssessmentResultDto? Budget { get; set; }
        public PolicyValidation? Validation { get; set; }
    }
}
