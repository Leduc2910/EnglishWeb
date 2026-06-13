using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Results;

namespace EnglishTestWeb.Api.Tests.Dashboard;

public sealed class TeacherDashboardTests
{
    private const string BaseUrl = "/api/teacher/dashboard";

    [Fact]
    public async Task GetDashboard_AsTeacher_ReturnsSummaryAndRecentWork()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("summary", out var summary));
        Assert.True(summary.GetProperty("templateCount").GetInt32() >= 1);
        Assert.True(summary.GetProperty("recentSubmissionCount").GetInt32() >= 1);

        Assert.True(root.TryGetProperty("recentWork", out var recentWork));
        Assert.True(recentWork.GetArrayLength() >= 1);
        var first = recentWork[0];
        Assert.True(first.TryGetProperty("type", out _));
        Assert.True(first.TryGetProperty("title", out _));
        Assert.True(first.TryGetProperty("mode", out _));
        Assert.True(first.TryGetProperty("status", out _));
        Assert.True(first.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task GetDashboard_WithClassFilter_FiltersMetrics()
    {
        await using var factory = new TestApiFactory();
        var (_, classId, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var resp = await client.GetAsync($"{BaseUrl}?classId={classId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("summary", out _));
        Assert.True(root.TryGetProperty("recentWork", out _));
    }

    [Fact]
    public async Task GetDashboard_NoData_ReturnsZeroCounts()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;
        var summary = root.GetProperty("summary");
        Assert.Equal(0, summary.GetProperty("templateCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("activeHomeworkCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("openLiveExamCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("recentSubmissionCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("pendingSpeakingCount").GetInt32());
        Assert.Equal(0, root.GetProperty("recentWork").GetArrayLength());
    }

    [Fact]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_AsStudent_Returns403()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);
        var resp = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
