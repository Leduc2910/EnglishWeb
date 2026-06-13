using EnglishTestWeb.Api.Contracts.Speaking;

namespace EnglishTestWeb.Api.Application.Speaking;

public interface ITeacherSpeakingGradingService
{
    Task<(bool Success, string? ErrorCode, TeacherSpeakingSubmissionDto? Dto)> GetForTeacherAsync(
        Guid speakingSubmissionId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorCode, TeacherSpeakingSubmissionDto? Dto)> GradeAsync(
        Guid speakingSubmissionId,
        string teacherId,
        GradeSpeakingRequest request,
        CancellationToken cancellationToken = default);
}
