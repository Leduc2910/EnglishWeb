using EnglishTestWeb.Api.Application.AssignedTests;
using EnglishTestWeb.Api.Contracts.AssignedTests;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.AssignedTests;

public sealed class AssignedTestService(
    EnglishTestWebDbContext db,
    TimeProvider timeProvider) : IAssignedTestService
{
    public async Task<IReadOnlyList<AssignedTestItem>> GetForStudentAsync(
        string studentId,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var homeworkItems = await (
            from h in db.HomeworkAssignments
            join t in db.TestTemplates on h.TestTemplateId equals t.Id
            join c in db.Classes on h.ClassId equals c.Id
            where h.ClassId == classId
            select new AssignedTestItem(
                h.Id,
                "homework",
                t.Title,
                t.Skill,
                h.ClassId,
                c.Name,
                h.Status,
                h.DeadlineAt >= now ? "available" : "expired",
                h.DeadlineAt,
                h.TimeLimitMinutes,
                null,
                null,
                null,
                h.CreatedAt))
            .ToListAsync(cancellationToken);

        var liveExamItems = await (
            from s in db.LiveExamSessions
            join t in db.TestTemplates on s.TestTemplateId equals t.Id
            join c in db.Classes on s.ClassId equals c.Id
            where s.ClassId == classId
            select new AssignedTestItem(
                s.Id,
                "live-exam",
                t.Title,
                t.Skill,
                s.ClassId,
                c.Name,
                s.Status,
                s.Status == LiveExamSessionStatuses.Scheduled ? "not-open"
                    : s.Status == LiveExamSessionStatuses.Open ? "available"
                    : "closed",
                null,
                null,
                s.ScheduledStartAt,
                s.OpenedAt,
                s.ClosedAt,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

        return homeworkItems
            .Concat(liveExamItems)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }
}
