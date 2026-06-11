using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

public sealed class MarkReadyControllerTests
{
    [Fact]
    public async Task MarkReady_DraftWithCompleteAnswerKey_ReturnsOkWithReadyStatus()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureDraftWithCompleteAnswerKeyAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;
        Assert.Equal("ready", root.GetProperty("status").GetString());
        Assert.Equal(templateId, root.GetProperty("templateId").GetGuid());
    }

    [Fact]
    public async Task MarkReady_AlreadyReady_ReturnsOkIdempotent()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        // POST twice
        var first = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });
        var second = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var body = await second.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("ready", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task MarkReady_ArchivedTemplate_Returns409()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureArchivedTemplateAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("templates.archived", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_MissingPdf_Returns400()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Draft template with no materials at all
        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("review.missingRequiredMaterial", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_MissingAnswerKey_Returns400()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Template has PDF but no AnswerKey
        var templateId = await TestTemplatesTestHelper.EnsureDraftWithMaterialsAsync(factory, "reading");
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("review.answerKeyIncomplete", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_IncompleteAnswerRows_Returns400()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Create draft with materials and a partial answer key
        var templateId = await TestTemplatesTestHelper.EnsureDraftWithMaterialsAsync(factory, "reading");

        // Add answer key with fewer rows than questionCount (partial)
        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<
                EnglishTestWeb.Api.Infrastructure.Persistence.EnglishTestWebDbContext>();

            var hasKey = await dbContext.AnswerKeyVersions.AnyAsync(a => a.TemplateId == templateId);
            if (!hasKey)
            {
                var rows = new[] { new { QuestionNumber = 1, CorrectAnswer = "A", Score = (decimal?)null } };
                var now = DateTimeOffset.UtcNow;
                dbContext.AnswerKeyVersions.Add(new EnglishTestWeb.Api.Domain.TestTemplates.AnswerKeyVersion
                {
                    Id = Guid.NewGuid(),
                    TemplateId = templateId,
                    Status = EnglishTestWeb.Api.Domain.TestTemplates.AnswerKeyStatuses.Draft,
                    ScoringMode = EnglishTestWeb.Api.Domain.TestTemplates.ScoringModes.Equal,
                    QuestionCount = 3, // 3 expected but only 1 row
                    TotalScore = 9m,
                    RowsJson = JsonSerializer.Serialize(rows, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await dbContext.SaveChangesAsync();
            }
        }

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("review.answerKeyIncomplete", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_InvalidScoring_Returns400()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureDraftWithMaterialsAsync(factory, "reading");

        // Add answer key with equal mode but TotalScore = null (invalid)
        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<
                EnglishTestWeb.Api.Infrastructure.Persistence.EnglishTestWebDbContext>();

            var hasKey = await dbContext.AnswerKeyVersions.AnyAsync(a => a.TemplateId == templateId);
            if (!hasKey)
            {
                var rows = new[]
                {
                    new { QuestionNumber = 1, CorrectAnswer = "A", Score = (decimal?)null },
                    new { QuestionNumber = 2, CorrectAnswer = "B", Score = (decimal?)null }
                };
                var now = DateTimeOffset.UtcNow;
                dbContext.AnswerKeyVersions.Add(new EnglishTestWeb.Api.Domain.TestTemplates.AnswerKeyVersion
                {
                    Id = Guid.NewGuid(),
                    TemplateId = templateId,
                    Status = EnglishTestWeb.Api.Domain.TestTemplates.AnswerKeyStatuses.Draft,
                    ScoringMode = EnglishTestWeb.Api.Domain.TestTemplates.ScoringModes.Equal,
                    QuestionCount = 2,
                    TotalScore = null, // missing total score
                    RowsJson = JsonSerializer.Serialize(rows, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await dbContext.SaveChangesAsync();
            }
        }

        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("review.scoringInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_SpeakingWithMaterial_ReturnsOkReady()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        // Speaking template just needs a material, no answer key
        var templateId = await TestTemplatesTestHelper.EnsureDraftWithMaterialsAsync(factory, "speaking");
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("ready", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task MarkReady_SpeakingNoMaterial_Returns400()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureSpeakingDraftTemplateAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("review.missingRequiredMaterial", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_CrossTeacher_ReturnsNotFound()
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
    public async Task MarkReady_Anonymous_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task MarkReady_Student_ReturnsForbidden()
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
    public async Task MarkReady_AnswerKeyStatusBecomesReady_WhenTemplateMarkedReady()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureDraftWithCompleteAnswerKeyAsync(factory);
        var response = await AuthTestHelper.PostJsonAsync(client, $"/api/test-templates/{templateId}/mark-ready", new { });

        response.EnsureSuccessStatusCode();

        // Verify AnswerKeyVersion status is also "ready"
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<
            EnglishTestWeb.Api.Infrastructure.Persistence.EnglishTestWebDbContext>();
        var answerKey = await dbContext.AnswerKeyVersions.FirstAsync(a => a.TemplateId == templateId);
        Assert.Equal("ready", answerKey.Status);
    }
}
