using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Agents.Services;

public class MaintenanceAnalysisAgentService(
    CoreGridDbContext db,
    IMaintenanceAnalysisToolsService maintenanceAnalysisTools,
    IAgentWorkflowService workflowService) : IMaintenanceAnalysisAgentService
{
    public async Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await db.AgentWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId && w.OrganizationId == organizationId, cancellationToken);
        if (workflow is null) return null;

        if (workflow.Status is not (WorkflowStatus.PLANNING or WorkflowStatus.ANALYZING))
        {
            throw new ConflictException(
                $"Workflow is {workflow.Status} — the Maintenance Analysis Agent only runs from PLANNING or ANALYZING.",
                "invalid_status_transition");
        }

        var started = DateTimeOffset.UtcNow;
        var stats = await maintenanceAnalysisTools.ComputeFailureStatisticsAsync(organizationId, workflow.AssetId, cancellationToken)
            ?? throw new InvalidOperationException("Asset not found.");

        var now = DateTimeOffset.UtcNow;
        workflow.MaintenanceAnalysis = JsonSerializer.Serialize(stats);
        workflow.UpdatedAt = now;

        db.AgentExecutionSteps.Add(AgentExecutionSteps.MaintenanceAnalysisSucceeded(
            workflow.Id, stats, (int)Math.Max(0, (now - started).TotalMilliseconds), now));

        await db.SaveChangesAsync(cancellationToken);

        // Node 2 assembles facts only — it never proposes a recommendation
        // or moves the workflow's status; nodes 3/4 (or a human reviewer)
        // read MaintenanceAnalysis off the workflow from here on.
        return await workflowService.GetWorkflowByIdAsync(organizationId, workflowId, cancellationToken);
    }
}
