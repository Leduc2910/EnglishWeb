namespace EnglishTestWeb.Api.Application.Security;

public interface IClassAuthorizationService
{
    Task<AuthorizationDecision> CanTeacherViewClassAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> CanStudentAccessClassAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> RequireTeacherClassAccessAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> RequireStudentClassAccessAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default);
}
