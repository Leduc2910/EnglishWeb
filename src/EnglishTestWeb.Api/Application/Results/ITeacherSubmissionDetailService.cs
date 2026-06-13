using EnglishTestWeb.Api.Contracts.Results;

namespace EnglishTestWeb.Api.Application.Results;

public interface ITeacherSubmissionDetailService
{
    Task<(bool Success, string? ErrorCode, TeacherSubmissionDetailDto? Dto)> GetForTeacherAsync(
        Guid submissionId,
        string teacherId,
        CancellationToken cancellationToken = default);
}
