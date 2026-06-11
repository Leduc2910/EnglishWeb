using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;

namespace EnglishTestWeb.Api.Tests.LiveExamSessions;

public sealed class OpenCloseControllerTests
{
    [Fact]
    public async Task Open_ScheduledSession_Returns200WithOpenStatus()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("open", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("live-exam", document.RootElement.GetProperty("mode").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("allowedActions").GetArrayLength());
        Assert.Equal("close", document.RootElement.GetProperty("allowedActions")[0].GetString());
        Assert.False(document.RootElement.GetProperty("openedAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Open_AlreadyOpen_Returns409AlreadyOpen()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        // Second open attempt
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("liveExam.alreadyOpen", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Open_ClosedSession_Returns409SessionClosed()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/close", new { });

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("liveExam.sessionClosed", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Open_SessionNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("liveExam.sessionNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Open_Anonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Close_OpenSession_Returns200WithClosedStatus()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/close", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("closed", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("live-exam", document.RootElement.GetProperty("mode").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("allowedActions").GetArrayLength());
        Assert.False(document.RootElement.GetProperty("closedAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Close_AlreadyClosed_Returns409AlreadyClosed()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });
        await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/close", new { });

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/close", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("liveExam.alreadyClosed", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Close_ScheduledSession_Returns409SessionNotOpen()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var sessionId = await LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/close", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("liveExam.sessionNotOpen", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Close_SessionNotOwned_Returns404()
    {
        await using var factory = new TestApiFactory();
        await LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("liveExam.sessionNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Close_Anonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Open_Student_Returns403()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Close_Student_Returns403()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
