using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.TestTemplates;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Submissions;

public sealed class SubmissionsFinalSubmitTests
{
    // ---- POST /api/submissions/{id}/submit ----

    [Fact]
    public async Task FinalSubmit_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task FinalSubmit_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task FinalSubmit_AsOwnerStudent_Returns200WithResult()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;
        Assert.Equal(submissionId, root.GetProperty("submissionId").GetGuid());
        Assert.Equal("auto-graded", root.GetProperty("status").GetString());
        Assert.Equal("homework", root.GetProperty("mode").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("templateTitle").GetString()));
        Assert.NotEqual(default, root.GetProperty("submittedAt").GetDateTimeOffset());
        Assert.True(root.TryGetProperty("autoScore", out _));
        Assert.True(root.TryGetProperty("questionCount", out _));
        Assert.True(root.TryGetProperty("correctCount", out _));
    }

    [Fact]
    public async Task FinalSubmit_AsOtherStudent_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new { });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("submission.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task FinalSubmit_Idempotent_Returns200SameResult()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        var resp1 = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        await using var body1 = await resp1.Content.ReadAsStreamAsync();
        using var doc1 = await JsonDocument.ParseAsync(body1);
        var submittedAt1 = doc1.RootElement.GetProperty("submittedAt").GetDateTimeOffset();

        var resp2 = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        await using var body2 = await resp2.Content.ReadAsStreamAsync();
        using var doc2 = await JsonDocument.ParseAsync(body2);
        var submittedAt2 = doc2.RootElement.GetProperty("submittedAt").GetDateTimeOffset();

        Assert.Equal(submittedAt1, submittedAt2);
    }

    [Fact]
    public async Task FinalSubmit_AutoGrades_CorrectAnswer()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        // Autosave correct answer "A"
        var saveResp = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "A" } }
        });
        Assert.Equal(HttpStatusCode.NoContent, saveResp.StatusCode);

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(1, doc.RootElement.GetProperty("correctCount").GetInt32());
        Assert.Equal(10m, doc.RootElement.GetProperty("autoScore").GetDecimal());
    }

    [Fact]
    public async Task FinalSubmit_AutoGrades_WrongAnswer()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        // Autosave wrong answer "B"
        var saveResp = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "B" } }
        });
        Assert.Equal(HttpStatusCode.NoContent, saveResp.StatusCode);

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(0, doc.RootElement.GetProperty("correctCount").GetInt32());
        Assert.Equal(0m, doc.RootElement.GetProperty("autoScore").GetDecimal());
    }

    [Fact]
    public async Task FinalSubmit_NoAnswer_AutoGrades_ZeroScore()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        // Submit without autosaving any answer
        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(0, doc.RootElement.GetProperty("correctCount").GetInt32());
        Assert.Equal(0m, doc.RootElement.GetProperty("autoScore").GetDecimal());
        // questionCount comes from the snapped AnswerKeyVersion, not answered rows count
        Assert.Equal(1, doc.RootElement.GetProperty("questionCount").GetInt32());
    }

    [Fact]
    public async Task FinalSubmit_ExpiredHomework_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(
            factory,
            deadlineAt: DateTimeOffset.UtcNow.AddDays(-1));
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Cannot create submission for expired homework — seed directly
        Guid submissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var homework = await db.HomeworkAssignments.FindAsync(homeworkId);
            var now = DateTimeOffset.UtcNow;
            var sub = new EnglishTestWeb.Api.Domain.Submissions.Submission
            {
                Id = Guid.NewGuid(),
                StudentId = db.Users.First(u => u.Email == AuthTestHelper.StudentEmail).Id,
                HomeworkAssignmentId = homeworkId,
                Status = EnglishTestWeb.Api.Domain.Submissions.SubmissionStatuses.Draft,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Submissions.Add(sub);
            await db.SaveChangesAsync();
            submissionId = sub.Id;
        }

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("submission.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task FinalSubmit_ClosedLiveExam_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId, _) = await SubmissionsTestHelper.SeedLiveExamWithReadyTemplateAsync(
            factory,
            status: LiveExamSessionStatuses.Open);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, null, sessionId);

        // Close the session directly via DB
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var session = await db.LiveExamSessions.FindAsync(sessionId);
            session!.Status = LiveExamSessionStatuses.Closed;
            session.ClosedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("submission.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task FinalSubmit_AfterSubmit_AutosaveReturns409()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        var submitResp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);

        var saveResp = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "A" } }
        });

        Assert.Equal(HttpStatusCode.Conflict, saveResp.StatusCode);
        Assert.Equal("submission.notDraft", await AuthTestHelper.ReadProblemCodeAsync(saveResp));
    }

    [Fact]
    public async Task GetWorkspace_AfterFinalSubmit_ReturnsNonDraftStatus()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SubmissionsTestHelper.CreateSubmissionAsync(client, homeworkId, null);

        var submitResp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{submissionId}/submit", new { });
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);

        var workspaceResp = await client.GetAsync($"/api/submissions/{submissionId}/workspace");
        workspaceResp.EnsureSuccessStatusCode();
        await using var body = await workspaceResp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var status = doc.RootElement.GetProperty("status").GetString();
        Assert.NotEqual("draft", status);
    }
}
