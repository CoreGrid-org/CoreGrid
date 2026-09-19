namespace CoreGrid.Api.Domain;

// AgentApproval.Decision values (AI-13 to AI-20). Consolidated (Phase 4,
// §6.1) out of the raw literals AgentWorkflowService used for both the
// request-shape check and the switch that applies the decision.
public static class WorkflowDecisions
{
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string Revise = "REVISE";
}
