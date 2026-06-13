using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;

namespace EnglishTestWeb.Api.Tests.Speaking;

public sealed class TeacherSpeakingGradingTests
{
    private static string GetUrl(Guid id) => $"/api/teacher/speaking-submissions/{id}";
    private static string GradeUrl(Guid id) => $"/api/teacher/speaking-submissions/{id}/grade";
    private static string FileUrl(Guid id) => $"/api/teacher/speaking-submissions/{id}/file";

    [Fact]
    public async Task Get_AsTeacher_OwnsSubmission_Returns200WithDto()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync(GetUrl(submissionId));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(submissionId.ToString(), doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("submitted", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Grade_ValidScore_Returns200WithGradedDto()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new
        {
            score = 8,
            feedback = "Good effort"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal("graded", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(8, doc.RootElement.GetProperty("score").GetInt32());
        Assert.Equal("Good effort", doc.RootElement.GetProperty("feedback").GetString());
    }

    [Fact]
    public async Task Grade_ScoreOutOfRange_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new
        {
            score = 11,
            feedback = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("speaking.scoreInvalid", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Grade_NegativeScore_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new
        {
            score = -1,
            feedback = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("speaking.scoreInvalid", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Get_NonExistentSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (_, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync(GetUrl(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Grade_DraftSubmission_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmissionWithDraftAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new
        {
            score = 7,
            feedback = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("speaking.notSubmitted", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync(GetUrl(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Grade_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await AuthTestHelper.PostJsonAsync(client, GradeUrl(Guid.NewGuid()), new { score = 5 });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetFile_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync(FileUrl(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // Task 3: Cross-scope with real data

    [Fact]
    public async Task Get_NonOwnerTeacher_WithRealData_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(otherClient, ClassesTestHelper.OtherTeacherEmail, ClassesTestHelper.OtherTeacherPassword);
        var resp = await otherClient.GetAsync(GetUrl(submissionId));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Grade_NonOwnerTeacher_WithRealData_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(otherClient, ClassesTestHelper.OtherTeacherEmail, ClassesTestHelper.OtherTeacherPassword);
        var resp = await AuthTestHelper.PostJsonAsync(otherClient, GradeUrl(submissionId), new { score = 7, feedback = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    // Task 4: Grade save idempotency / mutability

    [Fact]
    public async Task Grade_CalledTwice_SameData_ReturnsSameResult()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var payload = new { score = 7, feedback = "Well done" };

        var resp1 = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), payload);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        await using var body1 = await resp1.Content.ReadAsStreamAsync();
        using var doc1 = await JsonDocument.ParseAsync(body1);
        Assert.Equal("graded", doc1.RootElement.GetProperty("status").GetString());
        Assert.Equal(7, doc1.RootElement.GetProperty("score").GetInt32());
        Assert.Equal("Well done", doc1.RootElement.GetProperty("feedback").GetString());

        var resp2 = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), payload);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        await using var body2 = await resp2.Content.ReadAsStreamAsync();
        using var doc2 = await JsonDocument.ParseAsync(body2);
        Assert.Equal("graded", doc2.RootElement.GetProperty("status").GetString());
        Assert.Equal(7, doc2.RootElement.GetProperty("score").GetInt32());
        Assert.Equal("Well done", doc2.RootElement.GetProperty("feedback").GetString());
    }

    [Fact]
    public async Task Grade_CalledTwice_DifferentData_UpdatesToLatest()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(studentClient);
        var studentId = await AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await SpeakingTestHelper.SeedSubmittedSpeakingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var resp1 = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new { score = 7, feedback = "First" });
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var resp2 = await AuthTestHelper.PostJsonAsync(client, GradeUrl(submissionId), new { score = 8, feedback = "Second" });
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        await using var body2 = await resp2.Content.ReadAsStreamAsync();
        using var doc2 = await JsonDocument.ParseAsync(body2);
        Assert.Equal(8, doc2.RootElement.GetProperty("score").GetInt32());
        Assert.Equal("Second", doc2.RootElement.GetProperty("feedback").GetString());

        // Confirm final state by GET
        var getResp = await client.GetAsync(GetUrl(submissionId));
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        await using var getBody = await getResp.Content.ReadAsStreamAsync();
        using var getDoc = await JsonDocument.ParseAsync(getBody);
        Assert.Equal("graded", getDoc.RootElement.GetProperty("status").GetString());
        Assert.Equal(8, getDoc.RootElement.GetProperty("score").GetInt32());
    }
}
