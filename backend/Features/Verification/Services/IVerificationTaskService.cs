using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IVerificationTaskService
{
    Task<PagedResult<VerificationTaskDto>> GetTasksAsync(
        Guid organizationId,
        Guid? assignedToUserId,
        VerificationTaskQueryParameters query,
        CancellationToken cancellationToken);

    // B18: a real single-row query, not GetTasksAsync(...).FirstOrDefault(...).
    Task<VerificationTaskDto?> CompleteTaskAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        bool currentUserCanActOnAnyTask,
        CompleteVerificationTaskRequest request,
        CancellationToken cancellationToken);
}
