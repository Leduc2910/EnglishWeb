using EnglishTestWeb.Api.Application.Classes;
using EnglishTestWeb.Api.Contracts.Classes;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Classes;

public sealed class ClassService(EnglishTestWebDbContext dbContext) : IClassService
{
    public async Task<ClassLookupResult> LookupByCodeAsync(
        string rawCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = ClassCodeNormalizer.Normalize(rawCode);
        if (normalized is null)
        {
            return new ClassLookupResult(false, null, "classes.codeNotFound");
        }

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .Include(entity => entity.Memberships)
            .FirstOrDefaultAsync(entity => entity.ClassCode == normalized, cancellationToken);

        if (schoolClass is null)
        {
            return new ClassLookupResult(false, null, "classes.codeNotFound");
        }

        if (!string.Equals(schoolClass.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            return new ClassLookupResult(false, null, "classes.codeInactive");
        }

        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == schoolClass.TeacherId, cancellationToken);

        var teacherDisplayName = teacher?.UserName
            ?? teacher?.Email
            ?? "Giáo viên";

        return new ClassLookupResult(
            true,
            new ClassLookupResponse(
                schoolClass.Id,
                schoolClass.Name,
                schoolClass.ClassCode,
                teacherDisplayName,
                schoolClass.Status),
            null);
    }

    public async Task<IReadOnlyList<ClassSummaryResponse>> GetTeacherClassesAsync(
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dbContext.Classes
            .AsNoTracking()
            .Where(entity => entity.TeacherId == teacherId)
            .OrderBy(entity => entity.Name)
            .Select(entity => new ClassSummaryResponse(
                entity.Id,
                entity.Name,
                entity.ClassCode,
                entity.Status,
                entity.Memberships.Count(membership =>
                    membership.Status == ClassStatuses.Active)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassAccessResult> GetClassDetailForTeacherAsync(
        Guid classId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .Include(entity => entity.Memberships)
            .FirstOrDefaultAsync(entity => entity.Id == classId, cancellationToken);

        if (schoolClass is null || schoolClass.TeacherId != teacherId)
        {
            return new ClassAccessResult(false, null, "classes.forbidden");
        }

        var studentIds = schoolClass.Memberships
            .Select(membership => membership.StudentId)
            .Distinct()
            .ToList();

        var students = await dbContext.Users
            .AsNoTracking()
            .Where(user => studentIds.Contains(user.Id))
            .ToListAsync(cancellationToken);

        var studentLookup = students.ToDictionary(user => user.Id);

        var studentResponses = schoolClass.Memberships
            .OrderBy(membership => membership.StudentId)
            .Select(membership =>
            {
                studentLookup.TryGetValue(membership.StudentId, out var student);
                var displayName = student?.UserName ?? student?.Email ?? membership.StudentId;
                return new ClassStudentResponse(
                    membership.StudentId,
                    displayName,
                    student?.Email,
                    membership.Status);
            })
            .ToList();

        return new ClassAccessResult(
            true,
            new ClassDetailResponse(
                schoolClass.Id,
                schoolClass.Name,
                schoolClass.ClassCode,
                schoolClass.Status,
                studentResponses),
            null);
    }

    public async Task<bool> HasActiveMembershipAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dbContext.ClassMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.ClassId == classId
                    && membership.StudentId == studentId
                    && membership.Status == ClassStatuses.Active,
                cancellationToken);
    }

    public async Task<SchoolClassContext?> GetActiveClassByCodeAsync(
        string rawCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = ClassCodeNormalizer.Normalize(rawCode);
        if (normalized is null)
        {
            return null;
        }

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.ClassCode == normalized, cancellationToken);

        if (schoolClass is null)
        {
            return null;
        }

        return new SchoolClassContext(
            schoolClass.Id,
            schoolClass.Name,
            schoolClass.ClassCode,
            schoolClass.Status);
    }
}
