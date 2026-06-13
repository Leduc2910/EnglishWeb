using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;

namespace EnglishTestWeb.Api.Tests.Results;

public sealed class TeacherResultsTests
{
    private const string BaseUrl = "/api/teacher/results";

    [Fact]
    public async Task GetResults_AsTeacher_NoFilters_Returns200WithItems()
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
        Assert.True(root.GetProperty("totalCount").GetInt32() >= 1);
        var items = root.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        var first = items[0];
        Assert.True(first.TryGetProperty("studentName", out _));
        Assert.True(first.TryGetProperty("className", out _));
        Assert.True(first.TryGetProperty("templateTitle", out _));
        Assert.True(first.TryGetProperty("skill", out _));
        Assert.True(first.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task GetResults_FilterBySkillSpeaking_OnlyReturnsSpeakingRows()
    {
        await using var factory = new TestApiFactory();
        // Seed Reading submission
        var (homeworkId, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);
        // Seed Speaking submission
        var (speakingHomeworkId, _) = await Speaking.SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);

        await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);
        await Speaking.SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, speakingHomeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync($"{BaseUrl}?skill=speaking");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        foreach (var item in items.EnumerateArray())
        {
            Assert.Equal("speaking", item.GetProperty("skill").GetString());
            Assert.Equal("speaking", item.GetProperty("type").GetString());
        }
    }

    [Fact]
    public async Task GetResults_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Filter by "submitted" — should include our seeded submission
        var resp = await client.GetAsync($"{BaseUrl}?status=submitted&skill=reading");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        foreach (var item in items.EnumerateArray())
        {
            Assert.Equal("submitted", item.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task GetResults_FilterByStudentQ_ReturnsMatchingStudents()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Search by known student email fragment
        const string q = "student";
        var resp = await client.GetAsync($"{BaseUrl}?q={q}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetResults_NoMatchingRows_Returns200WithEmptyItems()
    {
        await using var factory = new TestApiFactory();
        var (_, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        // No submissions seeded
        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Filter by non-existent student name
        var resp = await client.GetAsync($"{BaseUrl}?q=zzznomatch999xyz");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("needsGrading").GetInt32());
    }

    [Fact]
    public async Task GetResults_OutOfScopeData_NotReturned()
    {
        await using var factory = new TestApiFactory();
        // Seed data belonging to default teacher
        var (homeworkId, _, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

        // Sign in as OTHER teacher (not the owner of the homework)
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var resp = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        // Other teacher should not see the default teacher's submissions
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetResults_NeedsGradingCount_ReflectsSpeakingSubmitted()
    {
        await using var factory = new TestApiFactory();
        var (speakingHomeworkId, _) = await Speaking.SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        await Speaking.SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, speakingHomeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync($"{BaseUrl}?skill=speaking&status=submitted");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("needsGrading").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetResults_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetResults_AsStudent_Returns403()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var resp = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }
}
