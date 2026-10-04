using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public sealed record AssetActionRecommendation(string Recommendation, string Rationale);

public interface IAssetActionRecommendationEngine
{
    AssetActionRecommendation Propose(AssetComplianceStateDto complianceState, OrganizationPolicyFactsDto policy);
}
