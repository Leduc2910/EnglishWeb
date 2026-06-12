using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using EnglishTestWeb.Api.Tests.TestTemplates;

namespace EnglishTestWeb.Api.Tests.Submissions;

public sealed class SubmissionsControllerTests
{
    // ---- POST /api/submissions ----

    [Fact]
    public async Task PostSubmission_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostSubmission_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostSubmission_ValidHomework_Returns201WithSubmissionId()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("draft", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("homework", document.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task PostSubmission_ValidHomework_Idempotent_Returns200SameId()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var body1 = new { homeworkAssignmentId = homeworkId, liveExamSessionId = (Guid?)null };
        var first = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", body1);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await using var firstBody = await first.Content.ReadAsStreamAsync();
        using var firstDoc = await JsonDocument.ParseAsync(firstBody);
        var firstId = firstDoc.RootElement.GetProperty("id").GetGuid();

        var second = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", body1);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await using var secondBody = await second.Content.ReadAsStreamAsync();
        using var secondDoc = await JsonDocument.ParseAsync(secondBody);
        var secondId = secondDoc.RootElement.GetProperty("id").GetGuid();

        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task PostSubmission_ExpiredHomework_Returns422()
    {
        await using var factory = new TestApiFactory();
        var pastDeadline = DateTimeOffset.UtcNow.AddDays(-1);
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory, deadlineAt: pastDeadline);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("submission.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostSubmission_ValidOpenLiveExam_Returns201WithSubmissionId()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId, _) = await SubmissionsTestHelper.SeedLiveExamWithReadyTemplateAsync(
            factory, status: LiveExamSessionStatuses.Open);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = sessionId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("live-exam", document.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task PostSubmission_ClosedLiveExam_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId, _) = await SubmissionsTestHelper.SeedLiveExamWithReadyTemplateAsync(
            factory, status: LiveExamSessionStatuses.Closed);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = sessionId
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("submission.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostSubmission_BothSourcesProvided_Returns422InvalidSource()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("submission.invalidSource", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostSubmission_NeitherSourceProvided_Returns422InvalidSource()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("submission.invalidSource", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // ---- GET /api/submissions/{id}/workspace ----

    [Fact]
    public async Task GetWorkspace_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetWorkspace_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetWorkspace_AfterCreatingHomeworkSubmission_ReturnsFullWorkspace()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await SubmissionsTestHelper.SeedHomeworkWithReadyTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var createResponse = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        createResponse.EnsureSuccessStatusCode();
        await using var createBody = await createResponse.Content.ReadAsStreamAsync();
        using var createDoc = await JsonDocument.ParseAsync(createBody);
        var submissionId = createDoc.RootElement.GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/submissions/{submissionId}/workspace");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(submissionId, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("draft", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("homework", document.RootElement.GetProperty("mode").GetString());
        Assert.Equal("reading", document.RootElement.GetProperty("skill").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("questionCount").GetInt32());
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("pdfMaterialId").GetGuid());
        Assert.True(document.RootElement.TryGetProperty("answerRows", out var rows));
        Assert.Equal(0, rows.GetArrayLength());
    }

    [Fact]
    public async Task GetWorkspace_NonExistentSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("submission.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // ---- GET /api/submissions/{id}/materials/{fileId}/content ----

    [Fact]
    public async Task GetMaterialContent_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/materials/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetMaterialContent_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/materials/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetMaterialContent_WrongSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/materials/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("files.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
