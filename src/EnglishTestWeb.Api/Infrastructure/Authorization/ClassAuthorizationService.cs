using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Authorization;

public sealed class ClassAuthorizationService(EnglishTestWebDbContext dbContext) : IClassAuthorizationService
{
    public Task<AuthorizationDecision> CanTeacherViewClassAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default) =>
        RequireTeacherClassAccessAsync(classId, teacherId, cancellationToken);

    public Task<AuthorizationDecision> CanStudentAccessClassAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default) =>
        RequireStudentClassAccessAsync(classId, studentId, cancellationToken);

    public async Task<AuthorizationDecision> RequireTeacherClassAccessAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == classId, cancellationToken);

        if (schoolClass is null || schoolClass.TeacherId != teacherId)
        {
            return AuthorizationDecision.HiddenNotFound(
                "classes.notFound",
                AuthorizationDenialReason.ClassOwnership);
        }

        return AuthorizationDecision.Allow();
    }

    public async Task<AuthorizationDecision> RequireStudentClassAccessAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == classId, cancellationToken);

        if (schoolClass is null
            || !string.Equals(schoolClass.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            return AuthorizationDecision.HiddenNotFound(
                "classes.notFound",
                AuthorizationDenialReason.ClassNotFound);
        }

        var hasMembership = await dbContext.ClassMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.ClassId == classId
                    && membership.StudentId == studentId
                    && membership.Status == ClassStatuses.Active,
                cancellationToken);

        if (!hasMembership)
        {
            return AuthorizationDecision.HiddenNotFound(
                "classes.notFound",
                AuthorizationDenialReason.ClassMembership);
        }

        return AuthorizationDecision.Allow();
    }
}
