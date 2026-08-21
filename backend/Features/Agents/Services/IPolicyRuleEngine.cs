using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IPolicyRuleEngine
{
    PolicyValidation Evaluate(PolicyEvaluationFacts facts);
}
