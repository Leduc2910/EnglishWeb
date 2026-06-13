using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using EnglishTestWeb.Api.Tests.TestTemplates;

namespace EnglishTestWeb.Api.Tests.Security;

public sealed class ProblemDetailsContractTests
{
    [Fact]
    public async Task ErrorResponse_BusinessErrors_HaveApplicationProblemJsonContentType()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);

        using var teacherClient = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(teacherClient);

        // 404: resource not found — via hiddenResourceResponseFactory (ContentTypes explicitly set)
        var resp404 = await teacherClient.GetAsync($"/api/test-templates/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp404.StatusCode);
        Assert.Equal("application/problem+json", resp404.Content.Headers.ContentType?.MediaType);

        // 409: conflict — answer key on already-ready template
        var readyTemplateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var resp409 = await AuthTestHelper.PutJsonAsync(teacherClient,
            $"/api/test-templates/{readyTemplateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = (decimal?)10, rows = new[] { new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null } } });
        Assert.Equal(HttpStatusCode.Conflict, resp409.StatusCode);
        Assert.Equal("application/problem+json", resp409.Content.Headers.ContentType?.MediaType);

        // 400: bad request — answer key validation
        var draftTemplateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var resp400 = await AuthTestHelper.PutJsonAsync(teacherClient,
            $"/api/test-templates/{draftTemplateId}/answer-key",
            new { questionCount = 0, scoringMode = "equal", totalScore = (decimal?)null, rows = (object[]?)null });
        Assert.Equal(HttpStatusCode.BadRequest, resp400.StatusCode);
        Assert.Equal("application/problem+json", resp400.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ErrorResponse_Always_HasStableExtensionsCode()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);

        using var anonClient = factory.CreateClient();
        using var teacherClient = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(teacherClient);
        using var studentWithClassClient = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(studentWithClassClient, classId);

        // auth.unauthorized on 401
        var resp401 = await anonClient.GetAsync("/api/test-templates");
        var code401 = await AuthTestHelper.ReadProblemCodeAsync(resp401);
        Assert.False(string.IsNullOrEmpty(code401), "Expected non-empty extensions.code on 401");
        Assert.Equal("auth.unauthorized", code401);

        // auth.forbidden on 403
        var resp403 = await studentWithClassClient.GetAsync("/api/test-templates");
        var code403 = await AuthTestHelper.ReadProblemCodeAsync(resp403);
        Assert.False(string.IsNullOrEmpty(code403), "Expected non-empty extensions.code on 403");
        Assert.Equal("auth.forbidden", code403);

        // templates.notFound on 404 for unknown template
        var resp404 = await teacherClient.GetAsync($"/api/test-templates/{Guid.NewGuid()}");
        var code404 = await AuthTestHelper.ReadProblemCodeAsync(resp404);
        Assert.False(string.IsNullOrEmpty(code404), "Expected non-empty extensions.code on 404");
        Assert.Equal("templates.notFound", code404);

        // submission.notFound on 404 for unknown submission workspace (student)
        var resp404Sub = await studentWithClassClient.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");
        var code404Sub = await AuthTestHelper.ReadProblemCodeAsync(resp404Sub);
        Assert.False(string.IsNullOrEmpty(code404Sub), "Expected non-empty extensions.code on submission 404");
        Assert.Equal("submission.notFound", code404Sub);
    }

    [Fact]
    public async Task ErrorResponse_Never_ExposesStorageKeys()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);

        using var teacherClient = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(teacherClient);

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            otherClient,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var uploadResp = await TestTemplateMaterialsTestHelper.UploadPdfAsync(teacherClient, templateId);
        uploadResp.EnsureSuccessStatusCode();
        await using var uploadBody = await uploadResp.Content.ReadAsStreamAsync();
        using var uploadDoc = await JsonDocument.ParseAsync(uploadBody);
        var fileId = uploadDoc.RootElement.GetProperty("fileId").GetGuid();

        // cross-teacher 404 response for file endpoint
        var resp = await otherClient.GetAsync($"/api/files/{fileId}/content");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();

        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protected-storage", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LocalAppData", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AppData", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".pdf", body, StringComparison.OrdinalIgnoreCase);
    }
}
