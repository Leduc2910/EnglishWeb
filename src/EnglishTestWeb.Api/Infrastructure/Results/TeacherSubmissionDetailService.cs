using System.Text.Json;
using EnglishTestWeb.Api.Application.Results;
using EnglishTestWeb.Api.Contracts.Results;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Results;

public sealed class TeacherSubmissionDetailService(EnglishTestWebDbContext db)
    : ITeacherSubmissionDetailService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<(bool Success, string? ErrorCode, TeacherSubmissionDetailDto? Dto)> GetForTeacherAsync(
        Guid submissionId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.Submissions
            .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
            .Include(s => s.LiveExamSession).ThenInclude(l => l!.Template)
            .Include(s => s.Answers)
            .Where(s => s.Id == submissionId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "submission.notFound", null);

        var sourceTeacherId = submission.HomeworkAssignment?.TeacherId
                           ?? submission.LiveExamSession?.TeacherId;
        if (sourceTeacherId != teacherId)
            return (false, "submission.notFound", null);

        var template = submission.HomeworkAssignment?.Template
                    ?? submission.LiveExamSession?.Template;
        var classId  = submission.HomeworkAssignment?.ClassId
                    ?? submission.LiveExamSession?.ClassId
                    ?? Guid.Empty;
        var mode     = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";

        var studentName = await db.Users
            .Where(u => u.Id == submission.StudentId)
            .Select(u => u.UserName ?? u.Email ?? submission.StudentId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? submission.StudentId;

        var className = await db.Classes
            .Where(c => c.Id == classId)
            .Select(c => c.Name)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        Dictionary<int, AnswerKeyRow> correctAnswers = [];
        if (submission.AnswerKeyVersionId.HasValue)
        {
            var akv = await db.AnswerKeyVersions
                .Where(a => a.Id == submission.AnswerKeyVersionId.Value)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (akv is not null)
            {
                try
                {
                    var rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(akv.RowsJson, JsonOpts) ?? [];
                    correctAnswers = rows
                        .GroupBy(r => r.QuestionNumber)
                        .ToDictionary(g => g.Key, g => g.First());
                }
                catch (JsonException) { /* corrupt RowsJson — return empty correct answers */ }
            }
        }

        var answerRows = submission.Answers
            .OrderBy(a => a.QuestionNumber)
            .Select(a =>
            {
                correctAnswers.TryGetValue(a.QuestionNumber, out var akRow);
                return new TeacherAnswerRowDto(
                    QuestionNumber: a.QuestionNumber,
                    StudentAnswer:  a.Answer,
                    CorrectAnswer:  akRow?.CorrectAnswer ?? string.Empty,
                    IsCorrect:      a.IsCorrect,
                    Score:          a.Score);
            })
            .ToList();

        var dto = new TeacherSubmissionDetailDto(
            Id:            submission.Id,
            StudentName:   studentName,
            ClassName:     className,
            TemplateTitle: template?.Title ?? string.Empty,
            Skill:         template?.Skill ?? string.Empty,
            Mode:          mode,
            Status:        submission.Status,
            AutoScore:     submission.AutoScore,
            SubmittedAt:   submission.SubmittedAt,
            Answers:       answerRows);

        return (true, null, dto);
    }
}
