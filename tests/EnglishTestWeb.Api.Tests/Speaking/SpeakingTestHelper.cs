using System.Text.Json;
using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Speaking;

internal static class SpeakingTestHelper
{
    internal static async Task<(Guid homeworkId, Guid classId)> SeedSpeakingHomeworkAsync(
        TestApiFactory factory,
        DateTimeOffset? deadlineAt = null,
        bool withCueMaterial = false)
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
            Title = $"Speaking Test Template {templateId}",
            Skill = "speaking",
            Status = TemplateStatuses.Ready,
            CreatedAt = now,
            UpdatedAt = now,
        });

        if (withCueMaterial)
        {
            var cueFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = $"cue-{templateId}.pdf",
                OriginalFileName = "cue.pdf",
                ContentType = "application/pdf",
                SizeBytes = 64,
                OwnerUserId = teacherId,
                Status = StoredFileStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.StoredFiles.Add(cueFile);
            db.TestMaterials.Add(new TestMaterial
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                StoredFileId = cueFile.Id,
                Role = MaterialRoles.Cue,
                IsActive = true,
                CreatedAt = now
            });
        }

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

        return (homework.Id, classId);
    }

    internal static async Task<(Guid sessionId, Guid classId)> SeedSpeakingLiveExamAsync(
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
            Title = $"Speaking Live Exam Template {templateId}",
            Skill = "speaking",
            Status = TemplateStatuses.Ready,
            CreatedAt = now,
            UpdatedAt = now,
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

        return (session.Id, classId);
    }

    internal static async Task<Guid> CreateSpeakingSubmissionAsync(
        HttpClient client,
        Guid? homeworkAssignmentId,
        Guid? liveExamSessionId)
    {
        var response = await Auth.AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId,
            liveExamSessionId
        });
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    internal static MultipartFormDataContent CreateAudioFormFile(
        string contentType = "audio/webm",
        string fileName = "recording.webm",
        int sizeBytes = 1024)
    {
        var audioBytes = new byte[sizeBytes];
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    internal static async Task<Guid> SeedSubmittedSpeakingSubmissionAsync(
        TestApiFactory factory,
        Guid homeworkAssignmentId,
        string studentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var now = DateTimeOffset.UtcNow;

        var draftFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = $"submitted-{Guid.NewGuid()}.webm",
            OriginalFileName = "recording.webm",
            ContentType = "audio/webm",
            SizeBytes = 2048,
            OwnerUserId = studentId,
            Status = StoredFileStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.StoredFiles.Add(draftFile);

        var submission = new SpeakingSubmission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = homeworkAssignmentId,
            LiveExamSessionId = null,
            DraftStoredFileId = draftFile.Id,
            Status = SpeakingSubmissionStatuses.Submitted,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.SpeakingSubmissions.Add(submission);
        await db.SaveChangesAsync();

        return submission.Id;
    }

    /// <summary>
    /// Seeds a SpeakingSubmission with a DraftStoredFileId directly via DbContext.
    /// Used for tests that need to control source state independently of the upload endpoint.
    /// </summary>
    internal static async Task<Guid> SeedSubmissionWithDraftAsync(
        TestApiFactory factory,
        Guid homeworkAssignmentId,
        string studentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var now = DateTimeOffset.UtcNow;

        var draftFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = $"draft-{Guid.NewGuid()}.webm",
            OriginalFileName = "recording.webm",
            ContentType = "audio/webm",
            SizeBytes = 2048,
            OwnerUserId = studentId,
            Status = StoredFileStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.StoredFiles.Add(draftFile);

        var submission = new SpeakingSubmission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = homeworkAssignmentId,
            LiveExamSessionId = null,
            DraftStoredFileId = draftFile.Id,
            Status = SpeakingSubmissionStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.SpeakingSubmissions.Add(submission);
        await db.SaveChangesAsync();

        return submission.Id;
    }
}
