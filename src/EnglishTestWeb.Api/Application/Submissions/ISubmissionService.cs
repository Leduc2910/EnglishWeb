using EnglishTestWeb.Api.Contracts.Submissions;

namespace EnglishTestWeb.Api.Application.Submissions;

public interface ISubmissionService
{
    Task<CreateSubmissionResult> CreateOrResumeAsync(
        string studentId,
        Guid activeClassId,
        CreateSubmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<SubmissionWorkspaceDto?> GetWorkspaceAsync(
        Guid submissionId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<AutosaveAnswersResult> AutosaveAnswersAsync(
        Guid submissionId,
        string studentId,
        AutosaveAnswersRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CreateSubmissionResult(
    bool Success,
    Guid? SubmissionId,
    string? ErrorCode,
    bool Created);
