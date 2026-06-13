using EnglishTestWeb.Api.Application.Dashboard;
using EnglishTestWeb.Api.Contracts.Dashboard;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Dashboard;

public sealed class TeacherDashboardService(EnglishTestWebDbContext db)
    : ITeacherDashboardService
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(7);

    public async Task<TeacherDashboardDto> GetDashboardAsync(
        string teacherId,
        Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var recentCutoff = now - RecentWindow;

        var templateCount = await db.TestTemplates
            .Where(t => t.TeacherId == teacherId)
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeHomeworkCount = await db.HomeworkAssignments
            .Where(h => h.TeacherId == teacherId
                     && h.DeadlineAt > now
                     && (classId == null || h.ClassId == classId))
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var openLiveExamCount = await db.LiveExamSessions
            .Where(l => l.TeacherId == teacherId
                     && l.Status == LiveExamSessionStatuses.Open
                     && (classId == null || l.ClassId == classId))
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var recentSubmissionCount = await db.Submissions
            .Where(s => s.SubmittedAt != null
                     && s.SubmittedAt >= recentCutoff
                     && (
                         (s.HomeworkAssignment != null
                          && s.HomeworkAssignment.TeacherId == teacherId
                          && (classId == null || s.HomeworkAssignment.ClassId == classId))
                         ||
                         (s.LiveExamSession != null
                          && s.LiveExamSession.TeacherId == teacherId
                          && (classId == null || s.LiveExamSession.ClassId == classId))
                     ))
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingSpeakingCount = await db.SpeakingSubmissions
            .Where(ss => ss.Status == SpeakingSubmissionStatuses.Submitted
                      && (
                          (ss.HomeworkAssignment != null
                           && ss.HomeworkAssignment.TeacherId == teacherId
                           && (classId == null || ss.HomeworkAssignment.ClassId == classId))
                          ||
                          (ss.LiveExamSession != null
                           && ss.LiveExamSession.TeacherId == teacherId
                           && (classId == null || ss.LiveExamSession.ClassId == classId))
                      ))
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var recentSubmissions = await db.Submissions
            .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
            .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
            .Where(s => s.SubmittedAt != null
                     && (
                         (s.HomeworkAssignment != null
                          && s.HomeworkAssignment.TeacherId == teacherId
                          && (classId == null || s.HomeworkAssignment.ClassId == classId))
                         ||
                         (s.LiveExamSession != null
                          && s.LiveExamSession.TeacherId == teacherId
                          && (classId == null || s.LiveExamSession.ClassId == classId))
                     ))
            .OrderByDescending(s => s.SubmittedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var classIds = recentSubmissions
            .Select(s => s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty)
            .Distinct()
            .Where(id => id != Guid.Empty)
            .ToList();

        Dictionary<Guid, string> classNames = [];
        if (classIds.Count > 0)
        {
            classNames = await db.Classes
                .Where(c => classIds.Contains(c.Id))
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        }

        var recentWork = recentSubmissions.Select(s =>
        {
            var isHomework = s.HomeworkAssignmentId.HasValue;
            var assignedClassId = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;
            var className = classNames.GetValueOrDefault(assignedClassId, string.Empty);
            var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
            return new TeacherRecentWorkItemDto(
                Type:      "submission",
                Id:        s.Id.ToString(),
                Title:     template?.Title ?? string.Empty,
                ClassName: className,
                Mode:      isHomework ? "homework" : "live-exam",
                Status:    s.Status,
                Timestamp: s.SubmittedAt!.Value);
        }).ToList();

        return new TeacherDashboardDto(
            Summary: new TeacherDashboardSummaryDto(
                TemplateCount:         templateCount,
                ActiveHomeworkCount:   activeHomeworkCount,
                OpenLiveExamCount:     openLiveExamCount,
                RecentSubmissionCount: recentSubmissionCount,
                PendingSpeakingCount:  pendingSpeakingCount),
            RecentWork: recentWork);
    }
}
