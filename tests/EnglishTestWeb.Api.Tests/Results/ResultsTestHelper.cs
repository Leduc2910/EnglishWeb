using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Submissions;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Results;

internal static class ResultsTestHelper
{
    /// <summary>
    /// Seeds a HomeworkAssignment with a Reading template, using the demo class and teacher.
    /// Returns (homeworkId, classId, templateId).
    /// </summary>
    internal static async Task<(Guid homeworkId, Guid classId, Guid templateId)> SeedResultsHomeworkAsync(
        TestApiFactory factory)
    {
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var teacherId = db.Users.First(u => u.Email == Auth.AuthTestHelper.TeacherEmail).Id;
        var now = DateTimeOffset.UtcNow;

        var templateId = Guid.NewGuid();
        db.TestTemplates.Add(new TestTemplate
        {
            Id = templateId,
            TeacherId = teacherId,
            Title = $"Reading Test {templateId}",
            Skill = TemplateSkill.Reading,
            Status = TemplateStatuses.Ready,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var homework = new HomeworkAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = templateId,
            ClassId = classId,
            Status = HomeworkAssignmentStatuses.Published,
            DeadlineAt = now.AddDays(7),
            TimeLimitMinutes = null,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.HomeworkAssignments.Add(homework);
        await db.SaveChangesAsync();

        return (homework.Id, classId, templateId);
    }

    /// <summary>
    /// Seeds a Submission (Reading/Listening) with Status=Submitted, AutoScore=7.5.
    /// </summary>
    internal static async Task<Guid> SeedSubmittedReadingSubmissionAsync(
        TestApiFactory factory,
        Guid homeworkAssignmentId,
        string studentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var now = DateTimeOffset.UtcNow;
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = homeworkAssignmentId,
            LiveExamSessionId = null,
            AnswerKeyVersionId = null,
            Status = SubmissionStatuses.Submitted,
            SubmittedAt = now,
            AutoScore = 7.5m,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        return submission.Id;
    }

    /// <summary>
    /// Seeds a Submission with Status=Submitted and 2 SubmissionAnswer rows.
    /// </summary>
    internal static async Task<Guid> SeedSubmittedReadingSubmissionWithAnswersAsync(
        TestApiFactory factory,
        Guid homeworkAssignmentId,
        string studentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var now = DateTimeOffset.UtcNow;
        var submissionId = Guid.NewGuid();
        db.Submissions.Add(new Submission
        {
            Id = submissionId,
            StudentId = studentId,
            HomeworkAssignmentId = homeworkAssignmentId,
            LiveExamSessionId = null,
            AnswerKeyVersionId = null,
            Status = SubmissionStatuses.Submitted,
            SubmittedAt = now,
            AutoScore = 8.0m,
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.SubmissionAnswers.Add(new EnglishTestWeb.Api.Domain.Submissions.SubmissionAnswer
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            QuestionNumber = 1,
            Answer = "A",
            IsCorrect = true,
            Score = 1m,
            UpdatedAt = now,
        });
        db.SubmissionAnswers.Add(new EnglishTestWeb.Api.Domain.Submissions.SubmissionAnswer
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            QuestionNumber = 2,
            Answer = "B",
            IsCorrect = false,
            Score = 0m,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();
        return submissionId;
    }

    /// <summary>
    /// Gets the "other teacher" id (seeded by ClassesTestHelper.SeedDemoClassAsync).
    /// </summary>
    internal static async Task<string> GetOtherTeacherIdAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        return await db.Users
            .Where(u => u.Email == Classes.ClassesTestHelper.OtherTeacherEmail)
            .Select(u => u.Id)
            .FirstAsync();
    }
}
