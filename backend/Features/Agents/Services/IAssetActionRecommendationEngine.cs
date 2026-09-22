using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Defines the interface for generating asset action recommendations.
public interface IAssetActionRecommendationEngine
{
    AssetActionRecommendation Propose(AssetComplianceStateDto complianceState, OrganizationPolicyFactsDto policy);
}
