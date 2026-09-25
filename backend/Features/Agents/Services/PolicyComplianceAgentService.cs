using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Agents.Services;

public class PolicyComplianceAgentService(
    CoreGridDbContext db,
    IAgentToolsService agentTools,
    IAssetActionRecommendationEngine recommendationEngine,
    IAgentWorkflowService workflowService) : IPolicyComplianceAgentService
{
    public async Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await db.AgentWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new ConflictException(
                $"Workflow is {workflow.Status} — the Policy Compliance Agent only runs from PLANNING or ANALYZING.",
                "invalid_status_transition");
        }

        // Assemble facts via exactly the agent's own tool allow-list (§7.3/§7.4)
        // — same tools EvaluatePolicyAsync itself will call again once it
        // receives our proposed recommendation.
        var complianceState = await agentTools.GetAssetComplianceStateAsync(organizationId, workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Could not load the asset's compliance state.");

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Asset not found.");

        var policy = await agentTools.GetOrganizationPoliciesAsync(organizationId, asset.AssetTypeId, cancellationToken)
            ?? throw new BusinessRuleException("No organisation policy is configured — cannot run the agent.", "no_policy_configured");

        var proposal = recommendationEngine.Propose(complianceState, policy);

        // Map Node 3 (Budget Analysis) assessment facts if available, or degrade gracefully to null
        FinancialAssessmentFacts? financialAssessment = null;
        var proposedRecommendation = proposal.Recommendation;

        if (!string.IsNullOrEmpty(workflow.BudgetAnalysis))
        {
            var budgetResult = JsonSerializer.Deserialize<FinancialAssessmentResultDto>(workflow.BudgetAnalysis);
            if (budgetResult != null)
            {
                if (!string.IsNullOrWhiteSpace(budgetResult.ProposedRecommendation))
                {
                    proposedRecommendation = budgetResult.ProposedRecommendation;
                }

                decimal? topScore = budgetResult.RankedOptions.Count > 0
                    ? budgetResult.RankedOptions.Max(o => o.Score)
                    : null;

                decimal? projectedRepairCost = null;
                if (!string.IsNullOrEmpty(workflow.MaintenanceAnalysis))
                {
                    var maintenanceStats = JsonSerializer.Deserialize<CoreGrid.Api.Features.AgentTools.DTOs.FailureStatisticsDto>(workflow.MaintenanceAnalysis);
                    projectedRepairCost = maintenanceStats?.ProjectedNextTwelveMonthsCost;
                }

                financialAssessment = new FinancialAssessmentFacts
                {
                    RepairToReplaceRatio = budgetResult.RepairToReplaceRatio,
                    BudgetHeadroom = budgetResult.BudgetHeadroom,
                    ProjectedRepairCost = projectedRepairCost,
                    Confidence = topScore
                };
            }
        }

        db.AgentExecutionSteps.Add(new AgentExecutionStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            Agent = AgentNames.PolicyComplianceRecommendation,
            Sequence = 4,
            OutputSummary = $"Proposed {proposedRecommendation}: {proposal.Rationale}",
            Status = "SUCCESS",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        // Hand off to the same deterministic gate a human's manual
        // "Evaluate policy compliance" submission runs through.
        return await workflowService.EvaluatePolicyAsync(
            organizationId,
            workflowId,
            new EvaluatePolicyRequest
            {
                ProposedRecommendation = proposedRecommendation,
                FinancialAssessment = financialAssessment
            },
            cancellationToken);
    }
}
