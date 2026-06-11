using EnglishTestWeb.Api.Contracts.HomeworkAssignments;

namespace EnglishTestWeb.Api.Application.HomeworkAssignments;

public sealed record CreateHomeworkAssignmentResult(
    bool Allowed,
    HomeworkAssignmentResponse? Detail,
    string? ErrorCode,
    int StatusCode);

public interface IHomeworkAssignmentService
{
    Task<CreateHomeworkAssignmentResult> CreateAsync(
        string teacherId,
        CreateHomeworkAssignmentRequest request,
        CancellationToken cancellationToken = default);
}
