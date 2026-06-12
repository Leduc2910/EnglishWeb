using System.Text.Json;
using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Submissions;

internal static class SubmissionsTestHelper
{
    internal static async Task<(Guid homeworkId, Guid classId, Guid pdfFileId)> SeedHomeworkWithReadyTemplateAsync(
        TestApiFactory factory,
        DateTimeOffset? deadlineAt = null)
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
            Title = $"Submission Test Template {templateId}",
            Skill = "reading",
            Status = TemplateStatuses.Ready,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = $"sub-test-{templateId}.pdf",
            OriginalFileName = "test.pdf",
            ContentType = "application/pdf",
            SizeBytes = 128,
            OwnerUserId = teacherId,
            Status = StoredFileStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.StoredFiles.Add(storedFile);

        db.TestMaterials.Add(new TestMaterial
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            StoredFileId = storedFile.Id,
            Role = MaterialRoles.Pdf,
            IsActive = true,
            CreatedAt = now
        });

        var answerKeyRows = new[] { new { QuestionNumber = 1, CorrectAnswer = "A", Score = (decimal?)null } };
        var answerKeyVersion = new AnswerKeyVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Status = AnswerKeyStatuses.Ready,
            ScoringMode = ScoringModes.Equal,
            QuestionCount = 1,
            TotalScore = 10m,
            RowsJson = JsonSerializer.Serialize(answerKeyRows, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AnswerKeyVersions.Add(answerKeyVersion);

        var homework = new HomeworkAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = templateId,
            ClassId = classId,
            Status = HomeworkAssignmentStatuses.Published,
            DeadlineAt = deadlineAt ?? now.AddDays(7),
            TimeLimitMinutes = null,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.HomeworkAssignments.Add(homework);
        await db.SaveChangesAsync();

        return (homework.Id, classId, storedFile.Id);
    }

    internal static async Task<(Guid sessionId, Guid classId, Guid pdfFileId)> SeedLiveExamWithReadyTemplateAsync(
        TestApiFactory factory,
        string status = LiveExamSessionStatuses.Open)
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
            Title = $"Live Exam Sub Template {templateId}",
            Skill = "listening",
            Status = TemplateStatuses.Ready,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = $"live-sub-test-{templateId}.pdf",
            OriginalFileName = "test.pdf",
            ContentType = "application/pdf",
            SizeBytes = 128,
            OwnerUserId = teacherId,
            Status = StoredFileStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.StoredFiles.Add(storedFile);

        db.TestMaterials.Add(new TestMaterial
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            StoredFileId = storedFile.Id,
            Role = MaterialRoles.Pdf,
            IsActive = true,
            CreatedAt = now
        });

        var answerKeyRows = new[] { new { QuestionNumber = 1, CorrectAnswer = "B", Score = (decimal?)null } };
        db.AnswerKeyVersions.Add(new AnswerKeyVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Status = AnswerKeyStatuses.Ready,
            ScoringMode = ScoringModes.Equal,
            QuestionCount = 1,
            TotalScore = 10m,
            RowsJson = JsonSerializer.Serialize(answerKeyRows, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CreatedAt = now,
            UpdatedAt = now
        });

        var session = new LiveExamSession
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = templateId,
            ClassId = classId,
            Status = status,
            OpenedAt = status == LiveExamSessionStatuses.Open ? now.AddMinutes(-5) : null,
            ClosedAt = status == LiveExamSessionStatuses.Closed ? now.AddMinutes(-1) : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.LiveExamSessions.Add(session);
        await db.SaveChangesAsync();

        return (session.Id, classId, storedFile.Id);
    }
}
