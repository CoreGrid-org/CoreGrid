using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// SRS §7.3, graph node 2 (Maintenance Analysis Agent — Seneja Ramanayaka).
// In-process, no model call (§7.2.1 criterion 2 fails: closed-form
// statistics, no free-text input to interpret) — same shape as the Policy
// Compliance node. Unlike node 4, this node produces no recommendation of
// its own: it assembles repair count / MTBF / cost trend / 12-month
// projection facts (via the already-built get_maintenance_history and
// compute_failure_statistics tools) for nodes 3/4, or a human reviewer, to
// read off the workflow.
public interface IMaintenanceAnalysisAgentService
{
    Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken);
}
