using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using EnglishTestWeb.Api.Tests.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Security;

public sealed class AuthorizationMatrixTests
{
    [Fact]
    public async Task Unauthenticated_GetClasses_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/classes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_GetClassDetail_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherOwner_GetClassDetail_ReturnsRoster()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeacherNonOwner_GetClassDetail_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_GetCurrentClass_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync("/api/classes/current");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentMember_GetCurrentClass_ReturnsSummary()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/classes/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("ENG7A", document.RootElement.GetProperty("classCode").GetString());
    }

    [Fact]
    public async Task StudentMember_GetMe_ReturnsActiveClass()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.True(document.RootElement.TryGetProperty("activeClass", out var activeClass));
        Assert.Equal("ENG7A", activeClass.GetProperty("classCode").GetString());
    }

    [Fact]
    public async Task StudentWithoutClaim_GetCurrentClass_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await client.GetAsync("/api/classes/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_LookupByCode_ReturnsPreview()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/classes/by-code/{ClassesTestHelper.ClassCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StudentStaleClaim_GetCurrentClass_ReturnsNotFoundAndMeOmitsActiveClass()
    {
        await using var factory = new AuditingTestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classBId = await ClassesTestHelper.SeedSecondClassWithoutMembershipAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classBId);

        var currentResponse = await client.GetAsync("/api/classes/current");
        Assert.Equal(HttpStatusCode.NotFound, currentResponse.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(currentResponse));
        Assert.Contains(
            factory.AuditLogger.Records,
            record =>
                record.ReasonCategory == EnglishTestWeb.Api.Application.Security.AuthorizationDenialReason.ClassMembership
                && record.ResourceType == "class");

        var meResponse = await client.GetAsync("/api/auth/me");
        await using var body = await meResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.False(document.RootElement.TryGetProperty("activeClass", out _));
    }

    [Fact]
    public async Task StudentStaleClaim_GetClassDetail_ReturnsForbiddenWithoutRoster()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classBId = await ClassesTestHelper.SeedSecondClassWithoutMembershipAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classBId);

        var response = await client.GetAsync($"/api/classes/{classBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("students", bodyText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StudentMember_GetTeacherClassList_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/classes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentMember_GetTeacherPing_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/auth/teacher/ping");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_GetNonExistentClass_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/classes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentMember_GetClassDetail_ReturnsForbiddenWithoutRoster()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("students", bodyText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InactiveClassWithActiveMembership_GetCurrentClass_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var schoolClass = await dbContext.Classes.FirstAsync(entity => entity.Id == classId);
            schoolClass.Status = EnglishTestWeb.Api.Domain.Classes.ClassStatuses.Inactive;
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/classes/current");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));

        var meResponse = await client.GetAsync("/api/auth/me");
        await using var body = await meResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.False(document.RootElement.TryGetProperty("activeClass", out _));
    }

    [Fact]
    public async Task RevokedMembership_GetCurrentClass_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var student = await dbContext.Users.FirstAsync(user => user.Email == AuthTestHelper.StudentEmail);
            var membership = await dbContext.ClassMemberships.FirstAsync(
                entry => entry.ClassId == classId && entry.StudentId == student.Id);
            membership.Status = EnglishTestWeb.Api.Domain.Classes.ClassStatuses.Inactive;
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/classes/current");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));

        var meResponse = await client.GetAsync("/api/auth/me");
        await using var body = await meResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.False(document.RootElement.TryGetProperty("activeClass", out _));
    }

    [Fact]
    public async Task StudentWithoutClaim_GetCurrentClass_EmitsAuditWithCorrelationId()
    {
        await using var factory = new AuditingTestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "matrix-corr-001");

        var response = await client.GetAsync("/api/classes/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            factory.AuditLogger.Records,
            record =>
                record.ReasonCategory == EnglishTestWeb.Api.Application.Security.AuthorizationDenialReason.ClassMembership
                && record.ResourceType == "class"
                && record.CorrelationId == "matrix-corr-001");
    }

    [Fact]
    public async Task Unauthenticated_GetTestTemplates_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test-templates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Student_GetTestTemplates_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await client.GetAsync("/api/test-templates");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_GetTestTemplateDetail_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherOwner_PostTestTemplate_ReturnsCreated()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Matrix Draft",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TeacherOwner_PutTestTemplate_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/test-templates/{templateId}", new
        {
            title = "Matrix Updated Draft",
            skill = "listening"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_PostTestTemplate_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Unauthorized Draft",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Student_PostTestTemplate_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Student Draft",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_PutTestTemplate_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/test-templates/{templateId}", new
        {
            title = "Blocked Update",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_GetTestTemplateDetail_EmitsAuditWithOwnershipReason()
    {
        await using var factory = new AuditingTestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            factory.AuditLogger.Records,
            record =>
                record.ReasonCategory == EnglishTestWeb.Api.Application.Security.AuthorizationDenialReason.TemplateOwnership
                && record.ResourceType == "test-template"
                && record.ResourceId == templateId.ToString());
    }

    [Fact]
    public async Task TeacherOwner_PostTestTemplateMaterial_ReturnsCreated()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TeacherOwner_GetFileContent_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var uploadResponse = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);
        uploadResponse.EnsureSuccessStatusCode();

        await using var uploadBody = await uploadResponse.Content.ReadAsStreamAsync();
        using var uploadDocument = await JsonDocument.ParseAsync(uploadBody);
        var fileId = uploadDocument.RootElement.GetProperty("fileId").GetGuid();

        var response = await client.GetAsync($"/api/files/{fileId}/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeacherNonOwner_GetFileContent_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var ownerClient = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(ownerClient);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var uploadResponse = await TestTemplateMaterialsTestHelper.UploadPdfAsync(ownerClient, templateId);
        uploadResponse.EnsureSuccessStatusCode();

        await using var uploadBody = await uploadResponse.Content.ReadAsStreamAsync();
        using var uploadDocument = await JsonDocument.ParseAsync(uploadBody);
        var fileId = uploadDocument.RootElement.GetProperty("fileId").GetGuid();

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            otherClient,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var response = await otherClient.GetAsync($"/api/files/{fileId}/content");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("files.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_PostTestTemplateMaterial_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_GetAnswerKey_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/test-templates/{templateId}/answer-key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Student_GetAnswerKey_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}/answer-key");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_GetAnswerKey_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}/answer-key");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_PutAnswerKey_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Student_PutAnswerKey_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherOwner_PutAnswerKeyOnDraft_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeacherOwner_PutAnswerKeyOnReady_ReturnsConflict()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("templates.notEditable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_PutAnswerKey_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 1, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task TeacherNonOwner_GetClassDetail_EmitsAuditWithOwnershipReason()
    {
        await using var factory = new AuditingTestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            factory.AuditLogger.Records,
            record =>
                record.ReasonCategory == EnglishTestWeb.Api.Application.Security.AuthorizationDenialReason.ClassOwnership
                && record.ResourceType == "class"
                && record.ResourceId == classId.ToString());
    }

    // mark-ready matrix rows
    [Fact]
    public async Task TeacherOwner_MarkReady_WithCompleteData_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureDraftWithCompleteAnswerKeyAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeacherOwner_MarkReady_AlreadyReady_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeacherNonOwner_MarkReady_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Student_MarkReady_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_MarkReady_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // --- Assigned Tests ---

    [Fact]
    public async Task Unauthenticated_GetAssignedTests_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_GetAssignedTests_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // --- Homework Assignments ---

    [Fact]
    public async Task Teacher_CreateHomeworkAssignment_WithValidData_Returns201()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignments.HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = DateTimeOffset.UtcNow.AddDays(7),
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Student_CreateHomeworkAssignment_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await HomeworkAssignments.HomeworkAssignmentTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId,
            classId,
            deadlineAt = DateTimeOffset.UtcNow.AddDays(7),
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CreateHomeworkAssignment_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/homework-assignments", new
        {
            templateId = Guid.NewGuid(),
            classId = Guid.NewGuid(),
            deadlineAt = DateTimeOffset.UtcNow.AddDays(7),
            timeLimitMinutes = (int?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Live Exam Sessions ---

    [Fact]
    public async Task Teacher_CreateLiveExamSession_WithValidData_Returns201()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessions.LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
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
    }

    [Fact]
    public async Task Student_CreateLiveExamSession_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessions.LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
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

    [Fact]
    public async Task Unauthenticated_CreateLiveExamSession_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId = Guid.NewGuid(),
            classId = Guid.NewGuid(),
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_OpenLiveExamSession_OwnSession_Returns200()
    {
        await using var factory = new TestApiFactory();
        var (templateId, classId) = await LiveExamSessions.LiveExamSessionTestHelper.EnsureReadyTemplateAndClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var sessionId = await LiveExamSessions.LiveExamSessionTestHelper.CreateScheduledSessionAsync(factory, client, templateId, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{sessionId}/open", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_OpenLiveExamSession_NonOwnedSession_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Student_OpenLiveExamSession_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_OpenLiveExamSession_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/open", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CloseLiveExamSession_NonOwnedSession_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Student_CloseLiveExamSession_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CloseLiveExamSession_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/live-exam-sessions/{Guid.NewGuid()}/close", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Submissions ---

    [Fact]
    public async Task Unauthenticated_PostSubmission_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_PostSubmission_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_GetSubmissionWorkspace_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_GetSubmissionWorkspace_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/workspace");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_GetSubmissionMaterialContent_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/materials/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Teacher_GetSubmissionMaterialContent_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/submissions/{Guid.NewGuid()}/materials/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // ---- PUT /api/submissions/{id}/answers ----

    [Fact]
    public async Task PutSubmissionAnswers_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await AuthTestHelper.EnsureXsrfAsync(client);

        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/answers", new
        {
            rows = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PutSubmissionAnswers_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/answers", new
        {
            rows = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_PostSubmissionSubmit_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }

    [Fact]
    public async Task Teacher_PostSubmissionSubmit_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);
        var resp = await AuthTestHelper.PostJsonAsync(client, $"/api/submissions/{Guid.NewGuid()}/submit", new { });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(resp));
    }
}
