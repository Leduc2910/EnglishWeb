using System.Net;
using System.Net.Http.Json;
using EnglishTestWeb.Api.Contracts.Results;

namespace EnglishTestWeb.Api.Tests.Results;

public class TeacherSubmissionDetailTests
{
    [Fact]
    public async Task GetSubmissionDetail_AsTeacher_ReturnsDetail()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, templateId) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await Auth.AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);
        var studentId = await Auth.AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await Auth.AuthTestHelper.SignInTeacherAsync(client);

        var resp = await client.GetAsync($"/api/teacher/results/submissions/{submissionId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<TeacherSubmissionDetailDto>();
        Assert.NotNull(dto);
        Assert.Equal(submissionId, dto.Id);
        Assert.Equal("submitted", dto.Status);
        Assert.Equal(7.5m, dto.AutoScore);
        Assert.Equal("homework", dto.Mode);
    }

    [Fact]
    public async Task GetSubmissionDetail_WithAnswers_ReturnsAnswerRows()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await Auth.AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);
        var studentId = await Auth.AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await ResultsTestHelper.SeedSubmittedReadingSubmissionWithAnswersAsync(
            factory, homeworkId, studentId);

        using var client = factory.CreateClient();
        await Auth.AuthTestHelper.SignInTeacherAsync(client);

        var resp = await client.GetAsync($"/api/teacher/results/submissions/{submissionId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<TeacherSubmissionDetailDto>();
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.Answers);
        Assert.Equal(1, dto.Answers[0].QuestionNumber);
    }

    [Fact]
    public async Task GetSubmissionDetail_NotFound_Returns404()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);

        using var client = factory.CreateClient();
        await Auth.AuthTestHelper.SignInTeacherAsync(client);

        var resp = await client.GetAsync($"/api/teacher/results/submissions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionDetail_OutOfScope_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId, _) = await ResultsTestHelper.SeedResultsHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await Auth.AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);
        var studentId = await Auth.AuthTestHelper.GetCurrentUserIdAsync(studentClient);
        var submissionId = await ResultsTestHelper.SeedSubmittedReadingSubmissionAsync(
            factory, homeworkId, studentId);

        // Non-existent submission ID = effectively out of scope (404)
        using var client = factory.CreateClient();
        await Auth.AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync($"/api/teacher/results/submissions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionDetail_Unauthenticated_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var resp = await client.GetAsync($"/api/teacher/results/submissions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionDetail_AsStudent_Returns403()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);

        using var client = factory.CreateClient();
        await Auth.AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var resp = await client.GetAsync($"/api/teacher/results/submissions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
