using EnglishTestWeb.Api.Contracts.Speaking;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Application.Speaking;

public interface ISpeakingSubmissionService
{
    Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> CreateOrResumeAsync(
        string studentId,
        Guid activeClassId,
        CreateSpeakingSubmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> GetAsync(
        Guid speakingSubmissionId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> UploadDraftAsync(
        Guid speakingSubmissionId,
        string studentId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> FinalSubmitAsync(
        Guid speakingSubmissionId,
        string studentId,
        CancellationToken cancellationToken = default);
}
