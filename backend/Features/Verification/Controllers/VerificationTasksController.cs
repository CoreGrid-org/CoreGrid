using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

[ApiController]
[Route("api/verification-tasks")]
[Authorize]
public class VerificationTasksController : CoreGridControllerBase
{
    private readonly IVerificationTaskService _taskService;

    public VerificationTasksController(
        IVerificationTaskService taskService,
        CoreGridDbContext db) : base(db)
    {
        _taskService = taskService;
    }

    // GET /api/verification-tasks?campaignId=&mine=&onlyPending=
    [HttpGet]
    public async Task<ActionResult<List<VerificationTaskDto>>> GetTasks(
        [FromQuery] Guid? campaignId,
        [FromQuery] bool mine,
        [FromQuery] bool onlyPending,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var tasks = await _taskService.GetTasksAsync(
            currentUser.OrganizationId,
            campaignId,
            mine ? currentUser.Id : null,
            onlyPending);

        return Ok(tasks);
    }

    // FR-059: complete a task by asserting presence/location/condition —
    // auto-raises discrepancies per FR-060 as a side effect.
    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<VerificationTaskDto>> CompleteTask(
        Guid id,
        [FromBody] CompleteVerificationTaskRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var canActOnAnyTask =
            currentUser.Role == CoreGridRole.Administrator || currentUser.Role == CoreGridRole.Auditor;

        try
        {
            var task = await _taskService.CompleteTaskAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                canActOnAnyTask,
                request);

            if (task is null) return NotFound(new { message = "Task not found." });

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
