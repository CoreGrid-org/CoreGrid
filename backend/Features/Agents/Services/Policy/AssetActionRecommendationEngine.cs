using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public sealed class AssetActionRecommendationEngine : IAssetActionRecommendationEngine
{
    public AssetActionRecommendation Propose(AssetComplianceStateDto state, OrganizationPolicyFactsDto policy)
    {
        var condition = state.CurrentCondition;
        var elapsed = $"elapsed service life ({state.ElapsedServiceLifeYears} years)";
        var minimum = $"the organisation's minimum ({policy.MinimumServiceLifeYears} years)";

        if (state.IsCondemned || condition == AssetConditions.Unserviceable)
        {
            return state.ElapsedServiceLifeYears >= policy.MinimumServiceLifeYears
                ? new(LifecycleActions.Dispose,
                    $"Condition is {condition} (condemned: {state.IsCondemned}) and {elapsed} has passed {minimum} — disposal is justified.")
                : new(LifecycleActions.Repair,
                    $"Condition is {condition}, but {elapsed} is still below {minimum} — too early to dispose, so repair is proposed instead.");
        }

        if (condition is AssetConditions.Poor or AssetConditions.Fair)
        {
            return new(LifecycleActions.Repair,
                $"Condition is {condition} — not severe enough to justify disposal or replacement outright, so repair is proposed.");
        }

        if (state.OpenTransferCount > 0)
        {
            return new(LifecycleActions.Transfer,
                $"Condition is {condition} with {state.OpenTransferCount} open transfer request(s) already recorded for this asset — completing the transfer is proposed.");
        }

        return new(LifecycleActions.Retain,
            $"Condition is {condition} with no open maintenance or transfer records — no lifecycle action is currently justified.");
    }
}
