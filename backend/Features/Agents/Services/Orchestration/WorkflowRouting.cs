using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Orchestration;

internal static class WorkflowRouting
{
    public const int MaxRevisions = 2;

    public static readonly WorkflowStatus[] ResumableStatuses =
    [
        WorkflowStatus.PLANNING, WorkflowStatus.ANALYZING, WorkflowStatus.VALIDATING
    ];

    public static readonly WorkflowStatus[] InFlightStatuses = [.. ResumableStatuses, WorkflowStatus.AWAITING_APPROVAL];

    public static string ApplyVerdict(AgentWorkflow workflow, PolicyValidation validation, bool automated)
    {
        var now = DateTimeOffset.UtcNow;
        workflow.UpdatedAt = now;
        var blockingReasons = string.Join(" ", validation.BlockingReasons);

        switch (validation.Verdict)
        {
            case PolicyVerdicts.Fail:
                Complete(workflow, WorkflowStatus.FAILED_SAFE, now, blockingReasons);
                return "Policy FAIL → safe failure. No business record was changed.";

            case PolicyVerdicts.NeedsRevision when automated || workflow.RevisionCount >= MaxRevisions:
                Complete(workflow, WorkflowStatus.REVISION_REQUESTED, now, blockingReasons);
                return "No policy-permitted action yet → revision requested. " + blockingReasons;

            case PolicyVerdicts.NeedsRevision:
                workflow.RevisionCount++;
                workflow.Status = WorkflowStatus.ANALYZING;
                return $"Needs revision → back to analysis (revision {workflow.RevisionCount} of {MaxRevisions}).";

            default:
                if (validation.IsHighImpact)
                {
                    workflow.Status = WorkflowStatus.AWAITING_APPROVAL;
                    workflow.ApprovalStatus = ApprovalStatus.PENDING;
                    return "High-impact recommendation → paused for Administrator approval.";
                }

                Complete(workflow, WorkflowStatus.COMPLETED_ADVISORY, now, failureReason: null);
                workflow.ApprovalStatus = ApprovalStatus.NOT_REQUIRED;
                return "Low-impact and policy-compliant → completed as advisory.";
        }
    }

    public static string? ApplyDecision(AgentWorkflow workflow, string decision, DateTimeOffset now)
    {
        workflow.UpdatedAt = now;
        switch (decision)
        {
            case WorkflowDecisions.Approve:
                workflow.Status = WorkflowStatus.APPROVED;
                workflow.ApprovalStatus = ApprovalStatus.APPROVED;
                workflow.CompletedAt = now;
                return null;

            case WorkflowDecisions.Reject:
                workflow.Status = WorkflowStatus.REJECTED;
                workflow.ApprovalStatus = ApprovalStatus.REJECTED;
                workflow.CompletedAt = now;
                return null;

            default:
                if (workflow.RevisionCount >= MaxRevisions)
                {
                    workflow.Status = WorkflowStatus.REVISION_REQUESTED;
                    workflow.CompletedAt = now;
                    return null;
                }

                workflow.RevisionCount++;
                workflow.Status = WorkflowStatus.ANALYZING;
                workflow.ApprovalStatus = ApprovalStatus.NOT_REQUIRED;
                return workflow.Recommendation;
        }
    }

    public static void FailSafe(AgentWorkflow workflow, string? reason) =>
        Complete(workflow, WorkflowStatus.FAILED_SAFE, DateTimeOffset.UtcNow, reason);

    private static void Complete(AgentWorkflow workflow, WorkflowStatus status, DateTimeOffset now, string? failureReason)
    {
        workflow.Status = status;
        workflow.FailureReason = failureReason;
        workflow.CompletedAt = now;
        workflow.UpdatedAt = now;
    }
}
