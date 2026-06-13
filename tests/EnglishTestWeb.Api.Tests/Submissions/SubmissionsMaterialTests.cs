using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.Submissions;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using EnglishTestWeb.Api.Tests.TestTemplates;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Submissions;

public sealed class SubmissionsMaterialTests
{
    private static async Task<(Guid submissionId, Guid pdfMaterialId, Guid classId)> SetupSubmissionWithRealPdfAsync(
        TestApiFactory factory)
    {
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);

        using var teacherClient = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(teacherClient);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);

        var uploadResp = await TestTemplateMaterialsTestHelper.UploadPdfAsync(teacherClient, templateId);
        uploadResp.EnsureSuccessStatusCode();

        var akResp = await AuthTestHelper.PutJsonAsync(teacherClient,
            $"/api/test-templates/{templateId}/answer-key", new
            {
                questionCount = 1,
                scoringMode = "equal",
                totalScore = (decimal?)10,
                rows = new[] { new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null } }
            });
        akResp.EnsureSuccessStatusCode();

        var markReadyResp = await AuthTestHelper.PostJsonAsync(teacherClient,
            $"/api/test-templates/{templateId}/mark-ready", new { });
        markReadyResp.EnsureSuccessStatusCode();

        var homeworkResp = await AuthTestHelper.PostJsonAsync(teacherClient, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = DateTimeOffset.UtcNow.AddDays(7),
            timeLimitMinutes = (int?)null
        });
        homeworkResp.EnsureSuccessStatusCode();
        await using var hwBody = await homeworkResp.Content.ReadAsStreamAsync();
        using var hwDoc = await JsonDocument.ParseAsync(hwBody);
        var homeworkId = hwDoc.RootElement.GetProperty("id").GetGuid();

        using var studentClient = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(studentClient, classId);

        var subResp = await AuthTestHelper.PostJsonAsync(studentClient, "/api/submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });
        subResp.EnsureSuccessStatusCode();
        await using var subBody = await subResp.Content.ReadAsStreamAsync();
        using var subDoc = await JsonDocument.ParseAsync(subBody);
        var submissionId = subDoc.RootElement.GetProperty("id").GetGuid();

        var workspaceResp = await studentClient.GetAsync($"/api/submissions/{submissionId}/workspace");
        workspaceResp.EnsureSuccessStatusCode();
        await using var wsBody = await workspaceResp.Content.ReadAsStreamAsync();
        using var wsDoc = await JsonDocument.ParseAsync(wsBody);
        var pdfMaterialId = wsDoc.RootElement.GetProperty("pdfMaterialId").GetGuid();

        return (submissionId, pdfMaterialId, classId);
    }

    [Fact]
    public async Task GetMaterialContent_StudentOwner_ReturnsPdfBytes()
    {
        await using var factory = new TestApiFactory();
        var (submissionId, pdfMaterialId, classId) = await SetupSubmissionWithRealPdfAsync(factory);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var resp = await client.GetAsync($"/api/submissions/{submissionId}/materials/{pdfMaterialId}/content");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal("bytes", resp.Headers.AcceptRanges?.ToString());
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-1.4", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task GetMaterialContent_WithRange_Returns206()
    {
        await using var factory = new TestApiFactory();
        var (submissionId, pdfMaterialId, classId) = await SetupSubmissionWithRealPdfAsync(factory);

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/submissions/{submissionId}/materials/{pdfMaterialId}/content");
        request.Headers.Range = new RangeHeaderValue(0, 7);
        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, resp.StatusCode);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.Equal(8, bytes.Length);
        Assert.Equal("%PDF-1.4", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task GetMaterialContent_OtherStudent_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (_, pdfMaterialId, classId) = await SetupSubmissionWithRealPdfAsync(factory);

        // Seed a submission owned by a different student directly
        Guid otherSubmissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var now = DateTimeOffset.UtcNow;
            var otherSub = new Submission
            {
                Id = Guid.NewGuid(),
                StudentId = "other-student-mat-" + Guid.NewGuid().ToString("N")[..8],
                HomeworkAssignmentId = null,
                Status = SubmissionStatuses.Draft,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Submissions.Add(otherSub);
            await db.SaveChangesAsync();
            otherSubmissionId = otherSub.Id;
        }

        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var resp = await client.GetAsync(
            $"/api/submissions/{otherSubmissionId}/materials/{pdfMaterialId}/content");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("files.notFound", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }
}
