using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared.Exceptions;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public sealed class PolicyComplianceEvaluator(
    IPolicyTools policyTools,
    IAssetActionRecommendationEngine recommendationEngine,
    IPolicyRuleEngine ruleEngine) : IPolicyComplianceEvaluator
{
    public async Task<PolicyOutcome> EvaluateAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto>? maintenanceByAsset,
        FinancialAssessmentResultDto? budget,
        PolicyEvaluationOptions options,
        CancellationToken cancellationToken)
    {
        var states = await policyTools.GetComplianceStateAsync(scope.Selection, cancellationToken);
        if (states.Count == 0)
        {
            throw new BusinessRuleException("There are no active assets in this evaluation's scope.", "empty_scope");
        }

        var policy = await policyTools.GetOrganizationPoliciesAsync(scope.OrganizationId, scope.AssetTypeId, cancellationToken)
            ?? throw new BusinessRuleException("No organisation policy is configured — cannot evaluate compliance.", "no_policy_configured");

        var context = new EvaluationContext(scope, policy, maintenanceByAsset, budget, options);
        var results = states.Select(state => EvaluateAsset(state, context)).ToList();

        return scope.IsSingleAsset ? PolicyOutcomeBuilder.ForAsset(results[0]) : PolicyOutcomeBuilder.ForFleet(scope, results);
    }

    private AssetEvaluation EvaluateAsset(AssetComplianceStateDto state, EvaluationContext context)
    {
        var triage = context.TriageById.GetValueOrDefault(state.AssetId);
        var conditionProposal = recommendationEngine.Propose(state, context.Policy);

        var evaluated = Candidates(context, state, triage, conditionProposal.Recommendation)
            .Select(action => new CandidateResult(action, ruleEngine.Evaluate(Facts(action, state, triage, context))))
            .ToList();

        var chosen = evaluated.FirstOrDefault(e => e.Validation.Verdict == PolicyVerdicts.Pass)
            ?? evaluated.FirstOrDefault(e => e.Validation.Verdict == PolicyVerdicts.NeedsRevision)
            ?? evaluated[0];

        var row = new FleetAssetResultDto
        {
            AssetId = state.AssetId,
            AssetCode = state.AssetCode,
            Condition = state.CurrentCondition,
            Action = chosen.Action,
            Verdict = chosen.Validation.Verdict,
            IsHighImpact = chosen.Validation.IsHighImpact,
            Ratio = triage?.Ratio,
            ProjectedCost = triage?.ProjectedCost ?? 0m,
            Reason = Reason(evaluated, chosen, conditionProposal)
        };
        return new AssetEvaluation(row, chosen.Validation);
    }

    private static List<string> Candidates(
        EvaluationContext context, AssetComplianceStateDto state, AssetFinancialTriageDto? triage, string conditionAction)
    {
        if (context.Options.ForcedAction is not null) return [context.Options.ForcedAction];

        var severe = state.IsCondemned || state.CurrentCondition == AssetConditions.Unserviceable;
        var conditionFirst = severe || triage is null || triage.Action == LifecycleActions.Retain;

        var ordered = new List<string?>();
        if (!severe) ordered.Add(context.ModelPick);
        ordered.AddRange(conditionFirst ? [conditionAction, triage?.Action] : [triage!.Action, conditionAction]);
        ordered.AddRange(context.RankedActions);
        ordered.Add(LifecycleActions.Retain);

        return ordered
            .OfType<string>()
            .Distinct()
            .Where(action => action != context.Options.ExcludedAction)
            .ToList();
    }

    private static PolicyEvaluationFacts Facts(
        string action, AssetComplianceStateDto state, AssetFinancialTriageDto? triage, EvaluationContext context)
    {
        var manual = context.Options.ManualFacts;
        return new PolicyEvaluationFacts
        {
            ProposedRecommendation = action,
            AssetCondition = state.CurrentCondition,
            AssetStatus = state.CurrentStatus,
            ElapsedServiceLifeYears = state.ElapsedServiceLifeYears,
            HasValuation = state.HasValuation,
            ValuationDate = state.ValuationDate,
            OpenMaintenanceCount = state.OpenMaintenanceCount,
            OpenTransferCount = state.OpenTransferCount,
            RepairToReplaceRatio = triage?.Ratio ?? manual?.RepairToReplaceRatio,
            ProjectedRepairCost = triage?.ProjectedCost
                ?? context.StatsById.GetValueOrDefault(state.AssetId)?.ProjectedNextTwelveMonthsCost
                ?? manual?.ProjectedRepairCost,
            BudgetHeadroom = context.Budget?.BudgetHeadroom ?? manual?.BudgetHeadroom,
            Confidence = context.Confidence,
            MinimumServiceLifeYears = context.Policy.MinimumServiceLifeYears,
            ValuationValidityWindowDays = context.Policy.ValuationValidityWindowDays,
            RepairToReplaceCostThreshold = context.Policy.RepairToReplaceCostThreshold,
            ConfidenceFloor = context.Policy.ConfidenceFloor,
            EvaluatedAsOf = context.AsOf
        };
    }

    private static string Reason(List<CandidateResult> evaluated, CandidateResult chosen, AssetActionRecommendation conditionProposal)
    {
        if (chosen.Validation.Verdict != PolicyVerdicts.Pass)
        {
            return string.Join(" ", chosen.Validation.BlockingReasons.DefaultIfEmpty("No candidate action is permitted by policy."));
        }

        var basis = chosen.Action == conditionProposal.Recommendation
            ? conditionProposal.Rationale
            : $"Financial triage favours {chosen.Action}.";

        var blocked = evaluated.TakeWhile(e => e.Action != chosen.Action).ToList();
        if (blocked.Count == 0) return basis;

        var blockedBy = blocked.Select(b =>
            $"{b.Action} ({string.Join(", ", b.Validation.RuleResults.Where(r => r.Outcome is PolicyVerdicts.Fail or PolicyVerdicts.NeedsRevision).Select(r => r.RuleId))})");
        return $"Revised from {string.Join(", ", blockedBy)} — {basis}";
    }

    private sealed record CandidateResult(string Action, PolicyValidation Validation);

    private sealed class EvaluationContext(
        EvaluationScope scope,
        OrganizationPolicyFactsDto policy,
        IReadOnlyList<FailureStatisticsDto>? maintenanceByAsset,
        FinancialAssessmentResultDto? budget,
        PolicyEvaluationOptions options)
    {
        public OrganizationPolicyFactsDto Policy { get; } = policy;
        public FinancialAssessmentResultDto? Budget { get; } = budget;
        public PolicyEvaluationOptions Options { get; } = options;
        public DateOnly AsOf { get; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public Dictionary<Guid, AssetFinancialTriageDto> TriageById { get; } = budget?.Assets?.ToDictionary(t => t.AssetId) ?? [];
        public Dictionary<Guid, FailureStatisticsDto> StatsById { get; } = maintenanceByAsset?.ToDictionary(s => s.AssetId) ?? [];
        public List<string> RankedActions { get; } = budget?.RankedOptions.OrderByDescending(o => o.Score).Select(o => o.Action).ToList() ?? [];

        public string? ModelPick { get; } = scope.IsSingleAsset && budget?.Source == AssessmentSources.Model ? budget.ProposedRecommendation : null;

        public decimal? Confidence => Options.ManualFacts?.Confidence ?? (ModelPick is not null ? Budget!.RankedOptions.Max(o => o.Score) : null);
    }
}
