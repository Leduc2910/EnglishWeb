using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.Submissions;
using EnglishTestWeb.Api.Contracts.Submissions;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Submissions;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Submissions;

public sealed class SubmissionService(
    EnglishTestWebDbContext db,
    TimeProvider timeProvider) : ISubmissionService
{
    public async Task<CreateSubmissionResult> CreateOrResumeAsync(
        string studentId,
        Guid activeClassId,
        CreateSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate exactly one source
        var hasHomework = request.HomeworkAssignmentId.HasValue;
        var hasSession = request.LiveExamSessionId.HasValue;

        if (hasHomework == hasSession)
        {
            return new CreateSubmissionResult(false, null, "submission.invalidSource", false);
        }

        var now = timeProvider.GetUtcNow();

        Guid templateId;

        if (hasHomework)
        {
            var homework = await db.HomeworkAssignments
                .AsNoTracking()
                .Where(h => h.Id == request.HomeworkAssignmentId!.Value && h.ClassId == activeClassId)
                .Select(h => new { h.DeadlineAt, h.TestTemplateId })
                .FirstOrDefaultAsync(cancellationToken);

            if (homework is null)
                return new CreateSubmissionResult(false, null, "submission.notFound", false);

            if (homework.DeadlineAt < now)
                return new CreateSubmissionResult(false, null, "submission.sourceUnavailable", false);

            templateId = homework.TestTemplateId;

            // Check idempotency
            var existingHw = await db.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId
                    && s.HomeworkAssignmentId == request.HomeworkAssignmentId!.Value
                    && s.Status == SubmissionStatuses.Draft)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingHw.HasValue)
                return new CreateSubmissionResult(true, existingHw.Value, null, false);
        }
        else
        {
            var session = await db.LiveExamSessions
                .AsNoTracking()
                .Where(s => s.Id == request.LiveExamSessionId!.Value && s.ClassId == activeClassId)
                .Select(s => new { s.Status, s.TestTemplateId, s.OpenedAt, s.ClosedAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (session is null)
                return new CreateSubmissionResult(false, null, "submission.notFound", false);

            if (session.Status != LiveExamSessionStatuses.Open)
                return new CreateSubmissionResult(false, null, "submission.sourceUnavailable", false);

            templateId = session.TestTemplateId;

            // Check idempotency
            var existingLe = await db.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId
                    && s.LiveExamSessionId == request.LiveExamSessionId!.Value
                    && s.Status == SubmissionStatuses.Draft)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingLe.HasValue)
                return new CreateSubmissionResult(true, existingLe.Value, null, false);
        }

        // Snap AnswerKeyVersionId
        var answerKeyVersionId = await db.AnswerKeyVersions
            .AsNoTracking()
            .Where(a => a.TemplateId == templateId && a.Status == AnswerKeyStatuses.Ready)
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = request.HomeworkAssignmentId,
            LiveExamSessionId = request.LiveExamSessionId,
            AnswerKeyVersionId = answerKeyVersionId,
            Status = SubmissionStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Submissions.Add(submission);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent request beat us to insertion — re-query for the existing draft.
            db.ChangeTracker.Clear();
            var existingRace = await db.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId
                    && (hasHomework
                        ? s.HomeworkAssignmentId == request.HomeworkAssignmentId!.Value
                        : s.LiveExamSessionId == request.LiveExamSessionId!.Value)
                    && s.Status == SubmissionStatuses.Draft)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingRace.HasValue)
                return new CreateSubmissionResult(true, existingRace.Value, null, false);

            return new CreateSubmissionResult(false, null, "submission.invalidSource", false);
        }

        return new CreateSubmissionResult(true, submission.Id, null, true);
    }

    public async Task<SubmissionWorkspaceDto?> GetWorkspaceAsync(
        Guid submissionId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var sub = await db.Submissions
            .AsNoTracking()
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .Select(s => new
            {
                s.Id,
                s.Status,
                s.HomeworkAssignmentId,
                s.LiveExamSessionId,
                s.AnswerKeyVersionId,
                HomeworkDeadlineAt = s.HomeworkAssignment != null ? (DateTimeOffset?)s.HomeworkAssignment.DeadlineAt : null,
                HomeworkTimeLimitMinutes = s.HomeworkAssignment != null ? s.HomeworkAssignment.TimeLimitMinutes : null,
                HomeworkClassId = s.HomeworkAssignment != null ? (Guid?)s.HomeworkAssignment.ClassId : null,
                HomeworkTemplateId = s.HomeworkAssignment != null ? (Guid?)s.HomeworkAssignment.TestTemplateId : null,
                SessionOpenedAt = s.LiveExamSession != null ? s.LiveExamSession.OpenedAt : null,
                SessionClosedAt = s.LiveExamSession != null ? s.LiveExamSession.ClosedAt : null,
                SessionClassId = s.LiveExamSession != null ? (Guid?)s.LiveExamSession.ClassId : null,
                SessionTemplateId = s.LiveExamSession != null ? (Guid?)s.LiveExamSession.TestTemplateId : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (sub is null) return null;

        var templateId = sub.HomeworkTemplateId ?? sub.SessionTemplateId;
        var classId = sub.HomeworkClassId ?? sub.SessionClassId;

        if (templateId is null || classId is null) return null;

        var mode = sub.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";

        // Load template and class info
        var templateInfo = await db.TestTemplates
            .AsNoTracking()
            .Where(t => t.Id == templateId.Value)
            .Select(t => new { t.Title, t.Skill })
            .FirstOrDefaultAsync(cancellationToken);

        if (templateInfo is null) return null;

        var classInfo = await db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId.Value)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (classInfo is null) return null;

        // Load answer key info
        var questionCount = 0;
        if (sub.AnswerKeyVersionId.HasValue)
        {
            questionCount = await db.AnswerKeyVersions
                .AsNoTracking()
                .Where(a => a.Id == sub.AnswerKeyVersionId.Value)
                .Select(a => a.QuestionCount)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Load materials
        var pdfMaterialId = await db.TestMaterials
            .AsNoTracking()
            .Where(m => m.TemplateId == templateId.Value && m.IsActive && m.Role == MaterialRoles.Pdf)
            .Select(m => (Guid?)m.StoredFileId)
            .FirstOrDefaultAsync(cancellationToken);

        if (pdfMaterialId is null) return null;

        var audioMaterialId = await db.TestMaterials
            .AsNoTracking()
            .Where(m => m.TemplateId == templateId.Value && m.IsActive && m.Role == MaterialRoles.Audio)
            .Select(m => (Guid?)m.StoredFileId)
            .FirstOrDefaultAsync(cancellationToken);

        return new SubmissionWorkspaceDto(
            sub.Id,
            sub.Status,
            mode,
            templateInfo.Title,
            templateInfo.Skill,
            classId.Value,
            classInfo.Name,
            sub.HomeworkAssignmentId,
            sub.LiveExamSessionId,
            sub.HomeworkDeadlineAt,
            sub.HomeworkTimeLimitMinutes,
            sub.SessionOpenedAt,
            sub.SessionClosedAt,
            pdfMaterialId.Value,
            audioMaterialId,
            questionCount,
            []);
    }
}
