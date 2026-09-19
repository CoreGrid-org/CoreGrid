using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Deterministic stand-in for what an LLM-based Policy Compliance Agent would
// otherwise propose — a small decision tree over the same facts
// PolicyRuleEngine itself gates on (condition, elapsed service life vs. the
// org's configured minimum, open maintenance/transfer records), so a
// well-behaved input tends to actually clear PR-01/PR-02 downstream instead
// of the recommendation and the gate talking past each other.
public class AssetActionRecommendationEngine : IAssetActionRecommendationEngine
{
    public AssetActionRecommendation Propose(AssetComplianceStateDto complianceState, OrganizationPolicyFactsDto policy)
    {
        var pastMinimumServiceLife = complianceState.ElapsedServiceLifeYears >= policy.MinimumServiceLifeYears;

        if (complianceState.IsCondemned || complianceState.CurrentCondition == AssetConditions.Unserviceable)
        {
            if (pastMinimumServiceLife)
            {
                return new AssetActionRecommendation(
                    "DISPOSE",
                    $"Condition is {complianceState.CurrentCondition} (condemned: {complianceState.IsCondemned}) and elapsed " +
                    $"service life ({complianceState.ElapsedServiceLifeYears} years) has passed the organisation's minimum " +
                    $"({policy.MinimumServiceLifeYears} years) — disposal is justified.");
            }

            return new AssetActionRecommendation(
                "REPAIR",
                $"Condition is {complianceState.CurrentCondition}, but elapsed service life " +
                $"({complianceState.ElapsedServiceLifeYears} years) is still below the organisation's minimum " +
                $"({policy.MinimumServiceLifeYears} years) — too early to dispose, so repair is proposed instead.");
        }

        if (complianceState.CurrentCondition is "POOR" or "FAIR")
        {
            return new AssetActionRecommendation(
                "REPAIR",
                $"Condition is {complianceState.CurrentCondition} — not severe enough to justify disposal or replacement " +
                "outright, so repair is proposed.");
        }

        if (complianceState.OpenTransferCount > 0)
        {
            return new AssetActionRecommendation(
                "TRANSFER",
                $"Condition is {complianceState.CurrentCondition} with {complianceState.OpenTransferCount} open transfer " +
                "request(s) already recorded for this asset — completing the transfer is proposed.");
        }

        return new AssetActionRecommendation(
            "RETAIN",
            $"Condition is {complianceState.CurrentCondition} with no open maintenance or transfer records — no " +
            "lifecycle action is currently justified.");
    }
}
