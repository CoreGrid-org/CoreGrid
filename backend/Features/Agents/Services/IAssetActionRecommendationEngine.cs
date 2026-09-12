using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// The Policy Compliance Agent's recommendation step (SRS §7.3, graph node
// 4). Tool allow-list is exactly get_organization_policies +
// get_asset_compliance_state (§7.4) — the caller assembles those facts via
// IAgentToolsService and passes them in here. Deliberately a pure function,
// same reasoning as IPolicyRuleEngine: no LLM call, no DB access, so "the
// same inputs always produce the same proposal" is provable and
// unit-testable without any surrounding infrastructure.
public interface IAssetActionRecommendationEngine
{
    AssetActionRecommendation Propose(AssetComplianceStateDto complianceState, OrganizationPolicyFactsDto policy);
}
