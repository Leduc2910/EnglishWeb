using System.Text.Json;
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
    private static readonly JsonSerializerOptions _rowsJsonOptions = new(JsonSerializerDefaults.Web);

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

        // Load saved draft answers
        var answerRows = await db.SubmissionAnswers
            .AsNoTracking()
            .Where(a => a.SubmissionId == submissionId)
            .OrderBy(a => a.QuestionNumber)
            .Select(a => new AnswerRowDto(a.QuestionNumber, a.Answer))
            .ToListAsync(cancellationToken);

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
            answerRows);
    }

    public async Task<AutosaveAnswersResult> AutosaveAnswersAsync(
        Guid submissionId,
        string studentId,
        AutosaveAnswersRequest request,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.Submissions
            .Include(s => s.Answers)
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return new AutosaveAnswersResult(false, "submission.notFound");

        // AC5: Reject autosave if already submitted
        if (submission.Status != SubmissionStatuses.Draft)
            return new AutosaveAnswersResult(false, "submission.notDraft");

        if (request.Rows is null or { Count: 0 })
            return new AutosaveAnswersResult(true, null);

        // Deduplicate by QuestionNumber — last value in list wins
        var deduped = request.Rows
            .GroupBy(r => r.QuestionNumber)
            .Select(g => g.Last())
            .ToList();

        var now = timeProvider.GetUtcNow();
        var existingMap = submission.Answers.ToDictionary(a => a.QuestionNumber);

        foreach (var row in deduped)
        {
            var answer = row.Answer?.Length > 500 ? row.Answer[..500] : row.Answer;

            if (existingMap.TryGetValue(row.QuestionNumber, out var existing))
            {
                existing.Answer = answer;
                existing.UpdatedAt = now;
            }
            else
            {
                db.SubmissionAnswers.Add(new SubmissionAnswer
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    QuestionNumber = row.QuestionNumber,
                    Answer = answer,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AutosaveAnswersResult(true, null);
    }

    public async Task<FinalSubmitResult> FinalSubmitAsync(
        Guid submissionId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.Submissions
            .Include(s => s.Answers)
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return new FinalSubmitResult(false, "submission.notFound", null);

        // AC4: Idempotency — already submitted → return existing result
        if (submission.Status != SubmissionStatuses.Draft)
        {
            var existingResult = await BuildResultDtoAsync(submission, cancellationToken);
            return new FinalSubmitResult(true, null, existingResult);
        }

        // AC5: Re-verify source is still open at submit time
        var now = timeProvider.GetUtcNow();
        if (submission.HomeworkAssignmentId.HasValue)
        {
            if (submission.HomeworkAssignment is null || submission.HomeworkAssignment.DeadlineAt < now)
                return new FinalSubmitResult(false, "submission.sourceUnavailable", null);
        }
        else
        {
            if (submission.LiveExamSession is null || submission.LiveExamSession.Status != LiveExamSessionStatuses.Open)
                return new FinalSubmitResult(false, "submission.sourceUnavailable", null);
        }

        // AC3: Auto-grade if AnswerKey version was snapped at submission creation
        decimal? autoScore = null;

        if (submission.AnswerKeyVersionId.HasValue)
        {
            var akv = await db.AnswerKeyVersions
                .AsNoTracking()
                .Where(a => a.Id == submission.AnswerKeyVersionId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (akv is not null)
            {
                List<AnswerKeyRow>? rows;
                try
                {
                    rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(akv.RowsJson, _rowsJsonOptions);
                }
                catch (JsonException)
                {
                    rows = null;
                }

                if (rows is not null && rows.Count > 0)
                {
                    // Deduplicate by QuestionNumber — last row wins (mirrors autosave logic)
                    var keyMap = rows
                        .GroupBy(r => r.QuestionNumber)
                        .ToDictionary(g => g.Key, g => g.Last());

                    var scorePerQuestion = akv.ScoringMode == ScoringModes.Equal && akv.QuestionCount > 0
                        ? (akv.TotalScore ?? 0m) / akv.QuestionCount
                        : 0m;

                    decimal totalEarned = 0m;
                    foreach (var answer in submission.Answers)
                    {
                        if (keyMap.TryGetValue(answer.QuestionNumber, out var keyRow))
                        {
                            var isCorrect = string.Equals(
                                answer.Answer?.Trim(),
                                keyRow.CorrectAnswer?.Trim() ?? string.Empty,
                                StringComparison.OrdinalIgnoreCase);

                            answer.IsCorrect = isCorrect;
                            answer.Score = isCorrect
                                ? (akv.ScoringMode == ScoringModes.PerQuestion
                                    ? keyRow.Score ?? 0m
                                    : scorePerQuestion)
                                : 0m;
                            totalEarned += answer.Score.Value;
                        }
                        else
                        {
                            answer.IsCorrect = false;
                            answer.Score = 0m;
                        }
                    }

                    autoScore = totalEarned;
                }
            }
        }

        // AutoGraded only when grading actually ran; Submitted otherwise
        submission.Status = autoScore.HasValue
            ? SubmissionStatuses.AutoGraded
            : SubmissionStatuses.Submitted;
        submission.SubmittedAt = now;
        submission.AutoScore = autoScore;
        submission.UpdatedAt = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent request won the race — re-query and return the committed result.
            db.ChangeTracker.Clear();
            var committed = await db.Submissions
                .Include(s => s.Answers)
                .Include(s => s.HomeworkAssignment)
                .Include(s => s.LiveExamSession)
                .AsNoTracking()
                .Where(s => s.Id == submissionId && s.StudentId == studentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (committed is null)
                return new FinalSubmitResult(false, "submission.notFound", null);

            var concurrentResult = await BuildResultDtoAsync(committed, cancellationToken);
            return new FinalSubmitResult(true, null, concurrentResult);
        }

        var result = await BuildResultDtoAsync(submission, cancellationToken);
        return new FinalSubmitResult(true, null, result);
    }

    private async Task<SubmissionResultDto> BuildResultDtoAsync(
        Submission submission,
        CancellationToken cancellationToken)
    {
        var templateId = submission.HomeworkAssignment?.TestTemplateId
            ?? submission.LiveExamSession?.TestTemplateId;

        var templateTitle = string.Empty;
        if (templateId.HasValue)
        {
            templateTitle = await db.TestTemplates
                .AsNoTracking()
                .Where(t => t.Id == templateId.Value)
                .Select(t => t.Title)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        var questionCount = submission.Answers.Count;
        if (submission.AnswerKeyVersionId.HasValue)
        {
            var akvCount = await db.AnswerKeyVersions
                .AsNoTracking()
                .Where(a => a.Id == submission.AnswerKeyVersionId.Value)
                .Select(a => (int?)a.QuestionCount)
                .FirstOrDefaultAsync(cancellationToken);
            questionCount = akvCount ?? questionCount;
        }

        var mode = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";
        var answeredCorrectly = submission.Answers.Count(a => a.IsCorrect == true);

        return new SubmissionResultDto(
            submission.Id,
            submission.Status,
            mode,
            templateTitle,
            submission.SubmittedAt ?? submission.UpdatedAt,
            submission.AutoScore,
            questionCount,
            answeredCorrectly);
    }
}
