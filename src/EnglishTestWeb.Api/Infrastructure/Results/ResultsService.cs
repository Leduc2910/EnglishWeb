using EnglishTestWeb.Api.Application.Results;
using EnglishTestWeb.Api.Contracts.Results;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Results;

public sealed class ResultsService(EnglishTestWebDbContext db) : IResultsService
{
    public async Task<ResultsPageDto> GetResultsForTeacherAsync(
        string teacherId,
        ResultsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(1, filter.Page);

        // --- Step 0: Pre-filter by student name if Q provided ---
        IReadOnlyList<string>? studentIdFilter = null;
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim().ToLower();
            studentIdFilter = await db.Users
                .Where(u => (u.UserName != null && u.UserName.ToLower().Contains(q)) ||
                            (u.Email    != null && u.Email.ToLower().Contains(q)))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (studentIdFilter.Count == 0)
                return new ResultsPageDto([], page, pageSize, 0, 0);
        }

        // --- Step 1: Query Submissions (Reading/Listening) ---
        // Skip entirely if skill filter explicitly selects "speaking"
        var rlRows = new List<ResultRowDto>();
        if (filter.Skill != "speaking")
        {
            var submissionQuery = db.Submissions
                .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                .Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.TeacherId == teacherId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.TeacherId == teacherId));

            if (filter.ClassId.HasValue)
                submissionQuery = submissionQuery.Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.ClassId == filter.ClassId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.ClassId == filter.ClassId));

            if (filter.Mode == "homework")
                submissionQuery = submissionQuery.Where(s => s.HomeworkAssignmentId != null);
            else if (filter.Mode == "live-exam")
                submissionQuery = submissionQuery.Where(s => s.LiveExamSessionId != null);

            if (filter.TemplateId.HasValue)
                submissionQuery = submissionQuery.Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.TestTemplateId == filter.TemplateId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.TestTemplateId == filter.TemplateId));

            if (!string.IsNullOrWhiteSpace(filter.Skill))
                submissionQuery = submissionQuery.Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.Template != null &&
                     s.HomeworkAssignment.Template.Skill == filter.Skill) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.Template    != null &&
                     s.LiveExamSession.Template.Skill == filter.Skill));

            if (!string.IsNullOrWhiteSpace(filter.Status))
                submissionQuery = submissionQuery.Where(s => s.Status == filter.Status);

            if (studentIdFilter != null)
                submissionQuery = submissionQuery.Where(s => studentIdFilter.Contains(s.StudentId));

            var submissions = await submissionQuery.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var s in submissions)
            {
                var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
                var mode     = s.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
                var classId  = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;

                rlRows.Add(new ResultRowDto(
                    Id:            s.Id,
                    Type:          "reading-listening",
                    Mode:          mode,
                    StudentName:   s.StudentId,
                    StudentId:     s.StudentId,
                    ClassId:       classId,
                    ClassName:     classId.ToString(),
                    TemplateId:    template?.Id ?? Guid.Empty,
                    TemplateTitle: template?.Title ?? string.Empty,
                    Skill:         template?.Skill ?? string.Empty,
                    Status:        s.Status,
                    Score:         s.AutoScore,
                    SubmittedAt:   s.SubmittedAt,
                    CreatedAt:     s.CreatedAt));
            }
        }

        // --- Step 2: Query SpeakingSubmissions ---
        // Skip entirely if skill filter explicitly selects "reading" or "listening"
        var speakingRows = new List<ResultRowDto>();
        if (filter.Skill == null || filter.Skill == "speaking")
        {
            var speakingQuery = db.SpeakingSubmissions
                .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
                .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
                .Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.TeacherId == teacherId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.TeacherId == teacherId));

            if (filter.ClassId.HasValue)
                speakingQuery = speakingQuery.Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.ClassId == filter.ClassId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.ClassId == filter.ClassId));

            if (filter.Mode == "homework")
                speakingQuery = speakingQuery.Where(s => s.HomeworkAssignmentId != null);
            else if (filter.Mode == "live-exam")
                speakingQuery = speakingQuery.Where(s => s.LiveExamSessionId != null);

            if (filter.TemplateId.HasValue)
                speakingQuery = speakingQuery.Where(s =>
                    (s.HomeworkAssignment != null && s.HomeworkAssignment.TestTemplateId == filter.TemplateId) ||
                    (s.LiveExamSession    != null && s.LiveExamSession.TestTemplateId == filter.TemplateId));

            if (!string.IsNullOrWhiteSpace(filter.Status))
                speakingQuery = speakingQuery.Where(s => s.Status == filter.Status);

            if (studentIdFilter != null)
                speakingQuery = speakingQuery.Where(s => studentIdFilter.Contains(s.StudentId));

            var speakings = await speakingQuery.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var s in speakings)
            {
                var template = s.HomeworkAssignment?.Template ?? s.LiveExamSession?.Template;
                var mode     = s.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
                var classId  = s.HomeworkAssignment?.ClassId ?? s.LiveExamSession?.ClassId ?? Guid.Empty;

                speakingRows.Add(new ResultRowDto(
                    Id:            s.Id,
                    Type:          "speaking",
                    Mode:          mode,
                    StudentName:   s.StudentId,
                    StudentId:     s.StudentId,
                    ClassId:       classId,
                    ClassName:     classId.ToString(),
                    TemplateId:    template?.Id ?? Guid.Empty,
                    TemplateTitle: template?.Title ?? string.Empty,
                    Skill:         "speaking",
                    Status:        s.Status,
                    Score:         s.Score.HasValue ? (decimal)s.Score.Value : null,
                    SubmittedAt:   s.SubmittedAt,
                    CreatedAt:     s.CreatedAt));
            }
        }

        // --- Step 3: Merge + resolve names (batch, no N+1) ---
        var allRows = rlRows.Concat(speakingRows).ToList();

        if (allRows.Count > 0)
        {
            var allStudentIds = allRows.Select(r => r.StudentId).Distinct().ToList();
            var allClassIds   = allRows.Select(r => r.ClassId).Distinct().ToList();

            var studentNames = await db.Users
                .Where(u => allStudentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id, cancellationToken);

            var classNames = await db.Classes
                .Where(c => allClassIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            allRows = allRows.Select(r => r with
            {
                StudentName = studentNames.GetValueOrDefault(r.StudentId, r.StudentId),
                ClassName   = classNames.GetValueOrDefault(r.ClassId, r.ClassId.ToString()),
            }).ToList();
        }

        // --- Step 4: Sort ---
        IOrderedEnumerable<ResultRowDto> sorted = filter.Sort switch
        {
            "studentName" => filter.Direction == "asc"
                ? allRows.OrderBy(r => r.StudentName)
                : allRows.OrderByDescending(r => r.StudentName),
            "score" => filter.Direction == "asc"
                ? allRows.OrderBy(r => r.Score)
                : allRows.OrderByDescending(r => r.Score),
            "status" => filter.Direction == "asc"
                ? allRows.OrderBy(r => r.Status)
                : allRows.OrderByDescending(r => r.Status),
            _ => filter.Direction == "asc"
                ? allRows.OrderBy(r => r.SubmittedAt)
                : allRows.OrderByDescending(r => r.SubmittedAt),
        };

        var sortedList = sorted.ThenBy(r => r.Id).ToList();

        // --- Step 5: Summary counts + Paginate ---
        var totalCount   = sortedList.Count;
        var needsGrading = sortedList.Count(r => r.Type == "speaking" && r.Status == SpeakingSubmissionStatuses.Submitted);
        var items        = sortedList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new ResultsPageDto(items, page, pageSize, totalCount, needsGrading);
    }
}
