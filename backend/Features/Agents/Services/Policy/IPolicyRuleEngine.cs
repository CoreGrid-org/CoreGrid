using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public interface IPolicyRuleEngine
{
    PolicyValidation Evaluate(PolicyEvaluationFacts facts);
}
