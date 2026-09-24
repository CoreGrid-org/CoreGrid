using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// Handles verification task operations.
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
    // Returns verification tasks.
    [HttpGet]
    [Authorize(Policy = Policies.CanVerifyAssets)]
    public async Task<ActionResult<PagedResult<VerificationTaskDto>>> GetTasks(
        [FromQuery] VerificationTaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var tasks = await _taskService.GetTasksAsync(
            currentUser.OrganizationId, query.Mine ? currentUser.Id : null, query, cancellationToken);

        return Ok(tasks);
    }

    // complete a task by asserting presence/location/condition —
    // auto-raises discrepancies per FR-060 as a side effect.
    [HttpPatch("{id:guid}/complete")]
    [Authorize(Policy = Policies.CanVerifyAssets)]
    public async Task<ActionResult<VerificationTaskDto>> CompleteTask(
        Guid id,
        [FromBody] CompleteVerificationTaskRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var canActOnAnyTask =
            currentUser.Role == CoreGridRole.Administrator || currentUser.Role == CoreGridRole.Auditor;

        var task = await _taskService.CompleteTaskAsync(
            currentUser.OrganizationId, id, currentUser.Id, canActOnAnyTask, request, cancellationToken);

        return task is null
            ? throw NotFoundException.For(nameof(VerificationTask), id)
            : Ok(task);
    }
}
