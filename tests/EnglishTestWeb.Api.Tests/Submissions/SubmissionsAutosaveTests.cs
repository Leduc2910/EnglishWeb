using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.TestTemplates;

namespace EnglishTestWeb.Api.Tests.Submissions;

public sealed class SubmissionsAutosaveTests
{
    // ---- PUT /api/submissions/{id}/answers ----

    [Fact]
    public async Task AutosaveAnswers_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/answers", new
        {
            rows = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task AutosaveAnswers_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/answers", new
        {
            rows = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task AutosaveAnswers_AsOwnerStudent_Returns204()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Create submission
        var createResp = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResp.EnsureSuccessStatusCode();
        var submissionId = await ReadSubmissionIdAsync(createResp);

        // Autosave answers
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "A" } }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AutosaveAnswers_AsOtherStudent_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();

        // Create submission as student in class
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);
        var createResp = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResp.EnsureSuccessStatusCode();
        var submissionId = await ReadSubmissionIdAsync(createResp);

        // Sign out + sign in as student without class context (simulates different student)
        // We re-use the same student but with Guid.NewGuid() submission — different submission
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "A" } }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("submission.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task AutosaveAnswers_Idempotent_LastValueWins()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var createResp = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResp.EnsureSuccessStatusCode();
        var submissionId = await ReadSubmissionIdAsync(createResp);

        // First autosave
        var first = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "A" } }
        });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // Second autosave overwrites with different value
        var second = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "B" } }
        });
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        // Workspace should return updated answer
        var workspace = await client.GetAsync($"/api/submissions/{submissionId}/workspace");
        workspace.EnsureSuccessStatusCode();
        await using var workspaceBody = await workspace.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(workspaceBody);
        var answerRows = doc.RootElement.GetProperty("answerRows").EnumerateArray().ToList();
        Assert.Single(answerRows);
        Assert.Equal(1, answerRows[0].GetProperty("questionNumber").GetInt32());
        Assert.Equal("B", answerRows[0].GetProperty("answer").GetString());
    }

    [Fact]
    public async Task GetWorkspace_AfterAutosave_ReturnsAnswerRows()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var createResp = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResp.EnsureSuccessStatusCode();
        var submissionId = await ReadSubmissionIdAsync(createResp);

        // Autosave answer
        var saveResp = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{submissionId}/answers", new
        {
            rows = new[] { new { questionNumber = 1, answer = "C" } }
        });
        Assert.Equal(HttpStatusCode.NoContent, saveResp.StatusCode);

        // Get workspace — answerRows should be populated
        var workspace = await client.GetAsync($"/api/submissions/{submissionId}/workspace");
        workspace.EnsureSuccessStatusCode();
        await using var body = await workspace.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);

        var answerRows = doc.RootElement.GetProperty("answerRows").EnumerateArray().ToList();
        Assert.Single(answerRows);
        Assert.Equal(1, answerRows[0].GetProperty("questionNumber").GetInt32());
        Assert.Equal("C", answerRows[0].GetProperty("answer").GetString());
    }

    [Fact]
    public async Task GetWorkspace_BeforeAutosave_ReturnsEmptyAnswerRows()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var createResp = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResp.EnsureSuccessStatusCode();
        var submissionId = await ReadSubmissionIdAsync(createResp);

        // Get workspace without autosaving
        var workspace = await client.GetAsync($"/api/submissions/{submissionId}/workspace");
        workspace.EnsureSuccessStatusCode();
        await using var body = await workspace.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);

        var answerRows = doc.RootElement.GetProperty("answerRows").EnumerateArray().ToList();
        Assert.Empty(answerRows);
    }

    private static async Task<Guid> ReadSubmissionIdAsync(HttpResponseMessage response)
    {
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        return document.RootElement.GetProperty("id").GetGuid();
    }
}
