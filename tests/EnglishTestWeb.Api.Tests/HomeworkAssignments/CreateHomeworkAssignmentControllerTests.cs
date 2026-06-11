using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;

namespace EnglishTestWeb.Api.Tests.HomeworkAssignments;

public sealed class CreateHomeworkAssignmentControllerTests
{
    private static readonly DateTimeOffset FutureDeadline = DateTimeOffset.UtcNow.AddDays(7);

    [Fact]
    public async Task Create_WithValidData_Returns201WithCorrectFields()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;
        Assert.Equal(templateId, root.GetProperty("templateId").GetGuid());
        Assert.Equal(classId, root.GetProperty("classId").GetGuid());
        Assert.Equal("published", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("templateTitle", out _));
        Assert.True(root.TryGetProperty("className", out _));
    }

    [Fact]
    public async Task Create_WithTimeLimit_Returns201()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = 60
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(60, document.RootElement.GetProperty("timeLimitMinutes").GetInt32());
    }

    [Fact]
    public async Task Create_TemplateNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Use non-existent templateId
        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId = Guid.NewGuid(),
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("homework.templateNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TemplateDraft_Returns400TemplateNotReady()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureDraftTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.templateNotReady", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TemplateArchived_Returns400TemplateNotReady()
    {
        await using var factory = new TestApiFactory();
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var archivedTemplateId = await TestTemplates.TestTemplatesTestHelper.EnsureArchivedTemplateAsync(factory);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId = archivedTemplateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.templateNotReady", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_ClassNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (templateId, _) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Use non-existent classId
        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId = Guid.NewGuid(),
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("homework.classNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_PastDeadline_Returns400DeadlinePast()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.deadlinePast", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TimeLimitZero_Returns400TimeLimitInvalid()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.timeLimitInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_TimeLimitTooLarge_Returns400TimeLimitInvalid()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = 601
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.timeLimitInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_InactiveClass_Returns400ClassNotActive()
    {
        await using var factory = new TestApiFactory();
        var (templateId, inactiveClassId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndInactiveClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId = inactiveClassId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homework.classNotActive", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Student_Returns403()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = FutureDeadline,
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
