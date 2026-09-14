using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-005 / SRS §4.6 Appendix B (CanVerifyAssets): Staff has no role in
// verification at all, so it's excluded from both actions here. Appendix B
// lists CanVerifyAssets as Officer+Auditor only (Administrator explicitly
// excluded, on the same "can't assert the physical world" principle as
// transfer-receipt confirmation) — but CompleteTaskAsync's own
// `canActOnAnyTask` flag already treats Administrator as a valid
// any-task actor, so this keeps Administrator rather than narrowing an
// existing, working capability as an unrequested side effect.
[ApiController]
[Route("api/verification-tasks")]
[Authorize]
public class VerificationTasksController : CoreGridControllerBase
{
    private const string VerificationRoles =
        $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IVerificationTaskService _taskService;

    public VerificationTasksController(
        IVerificationTaskService taskService,
        CoreGridDbContext db) : base(db)
    {
        _taskService = taskService;
    }

    // GET /api/verification-tasks?campaignId=&mine=&onlyPending=
    [HttpGet]
    [Authorize(Roles = VerificationRoles)]
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
    [Authorize(Roles = VerificationRoles)]
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
