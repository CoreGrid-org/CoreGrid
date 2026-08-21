namespace CoreGrid.Api.Domain;

// SRS §7.5: durable, structured, inspectable state for the Asset Lifecycle
// Decision workflow. Plan/AgentOutputs/ToolCalls/ValidationResult are JSONB
// for flexibility (raw agent artefacts); the queryable facts below are
// typed columns so dashboards/reports don't have to parse JSON (DR pattern
// matches CampaignReportDto's own separation of aggregate vs. line-item
// data). AI-10: chain-of-thought, raw prompts/responses, credentials and
// tokens are never persisted here — only structured artefacts and
// summaries, written by whatever populates AgentOutputs/ToolCalls.
public class AgentWorkflow
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public required string Objective { get; set; }

    public WorkflowStatus Status { get; set; }

    public string? Plan { get; set; } // jsonb: ExecutionPlan.steps[]
    public string? AgentOutputs { get; set; } // jsonb: keyed by agent name
    public string? ToolCalls { get; set; } // jsonb: name/agent/outcome/duration/retries
    public string? ValidationResult { get; set; } // jsonb: verdict + per-rule outcomes

    public string? Recommendation { get; set; } // REPAIR | REPLACE | TRANSFER | DISPOSE | RETAIN
    public bool IsHighImpact { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; }
    public int RevisionCount { get; set; }

    public string? FailureReason { get; set; }

    public required string CorrelationId { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public User? InitiatedByUser { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<AgentExecutionStep> Steps { get; set; } = new List<AgentExecutionStep>();
    public ICollection<AgentApproval> Approvals { get; set; } = new List<AgentApproval>();
}

public enum WorkflowStatus
{
    PLANNING,
    ANALYZING,
    VALIDATING,
    AWAITING_APPROVAL,
    APPROVED,
    REJECTED,
    COMPLETED_ADVISORY,
    REVISION_REQUESTED,
    FAILED_SAFE
}

public enum ApprovalStatus
{
    NOT_REQUIRED,
    PENDING,
    APPROVED,
    REJECTED
}

// One row per node execution — Planner/Maintenance/Budget/Policy — so the
// trace can reconstruct why a recommendation was made from persisted
// artefacts alone (AI-12), independent of log retention.
public class AgentExecutionStep
{
    public Guid Id { get; set; }

    public Guid WorkflowId { get; set; }
    public AgentWorkflow? Workflow { get; set; }

    public required string Agent { get; set; }
    public int Sequence { get; set; }

    public string? InputHash { get; set; }
    public string? OutputSummary { get; set; }
    public int? DurationMs { get; set; }

    public required string Status { get; set; } // SUCCESS | FAILED
    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

// AI-13 to AI-20: the human-approval checkpoint decision and its snapshot.
public class AgentApproval
{
    public Guid Id { get; set; }

    public Guid WorkflowId { get; set; }
    public AgentWorkflow? Workflow { get; set; }

    public required string Decision { get; set; } // APPROVE | REJECT | REVISE

    public Guid DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }

    public required string Reason { get; set; }

    public string? WorkflowSnapshot { get; set; } // jsonb: state at the moment of decision

    public DateTimeOffset DecidedAt { get; set; }
}
