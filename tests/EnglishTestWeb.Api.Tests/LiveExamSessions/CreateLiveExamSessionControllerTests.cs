using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;

namespace EnglishTestWeb.Api.Tests.LiveExamSessions;

public sealed class CreateLiveExamSessionControllerTests
{
    [Fact]
    public async Task Create_WithValidData_Returns201WithScheduledStatus()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;
        Assert.Equal("scheduled", root.GetProperty("status").GetString());
        Assert.Equal(templateId, root.GetProperty("templateId").GetGuid());
        Assert.Equal(classId, root.GetProperty("classId").GetGuid());
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("templateTitle", out _));
        Assert.True(root.TryGetProperty("className", out _));
    }

    [Fact]
    public async Task Create_WithScheduledTimes_Returns201()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = DateTimeOffset.UtcNow.AddHours(3);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = start,
            scheduledEndAt = end
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("scheduled", document.RootElement.GetProperty("status").GetString());
        Assert.False(document.RootElement.GetProperty("scheduledStartAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_TemplateNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId = Guid.NewGuid(),
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("liveExam.templateNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TemplateDraft_Returns400TemplateNotReady()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureDraftTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("liveExam.templateNotReady", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TemplateArchived_Returns400TemplateNotReady()
    {
        await using var factory = new TestApiFactory();
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        var archivedTemplateId = await TestTemplates.TestTemplatesTestHelper.EnsureArchivedTemplateAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId = archivedTemplateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("liveExam.templateNotReady", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_ClassNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (templateId, _) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId = Guid.NewGuid(),
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("liveExam.classNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_ClassNotActive_Returns400ClassNotActive()
    {
        await using var factory = new TestApiFactory();
        var (templateId, inactiveClassId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndInactiveClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId = inactiveClassId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("liveExam.classNotActive", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Student_Returns403()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
