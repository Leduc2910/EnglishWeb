using EnglishTestWeb.Api.Contracts.Classes;

namespace EnglishTestWeb.Api.Application.Classes;

public sealed record ClassLookupResult(bool Found, ClassLookupResponse? Class, string? ErrorCode);

public sealed record ClassAccessResult(bool Allowed, ClassDetailResponse? Detail, string? ErrorCode);

public interface IClassService
{
    Task<ClassLookupResult> LookupByCodeAsync(string rawCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassSummaryResponse>> GetTeacherClassesAsync(
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<ClassAccessResult> GetClassDetailForTeacherAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveMembershipAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<SchoolClassContext?> GetActiveClassByCodeAsync(
        string rawCode,
        CancellationToken cancellationToken = default);

    Task<SchoolClassContext?> GetClassContextByIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default);
}

public sealed record SchoolClassContext(Guid ClassId, string ClassName, string ClassCode, string Status);
