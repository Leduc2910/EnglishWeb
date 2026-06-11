using EnglishTestWeb.Api.Contracts.LiveExamSessions;

namespace EnglishTestWeb.Api.Application.LiveExamSessions;

public sealed record CreateLiveExamSessionResult(
    bool Allowed,
    LiveExamSessionResponse? Detail,
    string? ErrorCode,
    int StatusCode);

public sealed record LiveExamSessionTransitionResult(
    bool Allowed,
    LiveExamSessionResponse? Detail,
    string? ErrorCode,
    int StatusCode);

public interface ILiveExamSessionService
{
    Task<CreateLiveExamSessionResult> CreateAsync(
        string teacherId,
        CreateLiveExamSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<LiveExamSessionTransitionResult> OpenAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<LiveExamSessionTransitionResult> CloseAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
