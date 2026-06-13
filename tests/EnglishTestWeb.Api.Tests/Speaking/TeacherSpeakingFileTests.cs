using System.Net;
using System.Net.Http.Headers;
using EnglishTestWeb.Api.Tests.Auth;

namespace EnglishTestWeb.Api.Tests.Speaking;

public sealed class TeacherSpeakingFileTests
{
    private static string FileUrl(Guid id) => $"/api/teacher/speaking-submissions/{id}/file";

    [Fact]
    public async Task GetFile_Owner_ReturnsAudioBytes()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);
        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            studentClient, homeworkId, null);
        var uploadContent = SpeakingTestHelper.CreateAudioFormFile();
        var uploadResp = await studentClient.PostAsync(
            $"/api/speaking-submissions/{submissionId}/upload-draft", uploadContent);
        uploadResp.EnsureSuccessStatusCode();

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await client.GetAsync(FileUrl(submissionId));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("audio/webm", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal("bytes", resp.Headers.AcceptRanges?.ToString());
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.Equal(1024, bytes.Length);
    }

    [Fact]
    public async Task GetFile_NonOwnerTeacher_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);
        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            studentClient, homeworkId, null);
        var uploadContent = SpeakingTestHelper.CreateAudioFormFile();
        var uploadResp = await studentClient.PostAsync(
            $"/api/speaking-submissions/{submissionId}/upload-draft", uploadContent);
        uploadResp.EnsureSuccessStatusCode();

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            otherClient,
            Classes.ClassesTestHelper.OtherTeacherEmail,
            Classes.ClassesTestHelper.OtherTeacherPassword);
        var resp = await otherClient.GetAsync(FileUrl(submissionId));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task GetFile_UnauthenticatedWithValidId_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync(FileUrl(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
