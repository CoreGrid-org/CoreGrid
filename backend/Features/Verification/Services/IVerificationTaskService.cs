using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IVerificationTaskService
{
    Task<List<VerificationTaskDto>> GetTasksAsync(
        Guid organizationId,
        Guid? campaignId,
        Guid? assignedToUserId,
        bool onlyPending);

    Task<VerificationTaskDto?> CompleteTaskAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        bool currentUserCanActOnAnyTask,
        CompleteVerificationTaskRequest request);
}
