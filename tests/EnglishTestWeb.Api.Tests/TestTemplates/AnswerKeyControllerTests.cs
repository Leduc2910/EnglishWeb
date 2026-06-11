using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

public sealed class AnswerKeyControllerTests
{
    [Fact]
    public async Task Get_WithoutAnswerKey_ReturnsNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}/answer-key");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("answerKey.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_ValidEqualMode_ReturnsOkWithDraftStatus()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            BuildRequest(questionCount: 3, scoringMode: "equal", totalScore: 9m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;
        Assert.Equal(templateId, root.GetProperty("templateId").GetGuid());
        Assert.Equal("draft", root.GetProperty("status").GetString());
        Assert.Equal("equal", root.GetProperty("scoringMode").GetString());
        Assert.Equal(3, root.GetProperty("questionCount").GetInt32());
        Assert.Equal(9m, root.GetProperty("totalScore").GetDecimal());
        Assert.Equal(3, root.GetProperty("rows").GetArrayLength());
        Assert.NotEqual(Guid.Empty, root.GetProperty("answerKeyVersionId").GetGuid());
    }

    [Fact]
    public async Task Put_Twice_UpdatesWithoutDuplicate()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var url = $"/api/test-templates/{templateId}/answer-key";

        var first = await AuthTestHelper.PutJsonAsync(
            client,
            url,
            BuildRequest(questionCount: 2, scoringMode: "equal", totalScore: 10m));
        first.EnsureSuccessStatusCode();

        var second = await AuthTestHelper.PutJsonAsync(
            client,
            url,
            BuildRequest(questionCount: 4, scoringMode: "per-question", totalScore: null, perQuestionScore: 2.5m));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var count = await dbContext.AnswerKeyVersions.CountAsync(entity => entity.TemplateId == templateId);
            Assert.Equal(1, count);
        }

        await using var body = await second.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(4, document.RootElement.GetProperty("questionCount").GetInt32());
        Assert.Equal("per-question", document.RootElement.GetProperty("scoringMode").GetString());
    }

    [Fact]
    public async Task Get_AfterPut_ReturnsPersistedRows()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var url = $"/api/test-templates/{templateId}/answer-key";

        var put = await AuthTestHelper.PutJsonAsync(
            client,
            url,
            BuildRequest(questionCount: 2, scoringMode: "equal", totalScore: 8m));
        put.EnsureSuccessStatusCode();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var rows = document.RootElement.GetProperty("rows");
        Assert.Equal(2, rows.GetArrayLength());
        Assert.Equal(1, rows[0].GetProperty("questionNumber").GetInt32());
        Assert.Equal("A", rows[0].GetProperty("correctAnswer").GetString());
        Assert.Equal(8m, document.RootElement.GetProperty("totalScore").GetDecimal());
    }

    [Fact]
    public async Task Put_QuestionCountZero_ReturnsInvalidQuestionCount()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 0, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.questionCount", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_QuestionCountAboveLimit_ReturnsInvalidQuestionCount()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 201, scoringMode = "equal", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.questionCount", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_InvalidScoringMode_ReturnsInvalidScoringMode()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new { questionCount = 2, scoringMode = "weighted", totalScore = 10, rows = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.scoringMode", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_RowCountMismatch_ReturnsInvalidRowCount()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new
            {
                questionCount = 3,
                scoringMode = "equal",
                totalScore = 9,
                rows = new[] { new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null } }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.rowCount", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_DuplicateRowNumber_ReturnsInvalidRowNumber()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new
            {
                questionCount = 2,
                scoringMode = "equal",
                totalScore = 10,
                rows = new[]
                {
                    new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null },
                    new { questionNumber = 1, correctAnswer = "B", score = (decimal?)null }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.rowNumber", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_RowNumberOutOfRange_ReturnsInvalidRowNumber()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new
            {
                questionCount = 2,
                scoringMode = "equal",
                totalScore = 10,
                rows = new[]
                {
                    new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null },
                    new { questionNumber = 5, correctAnswer = "B", score = (decimal?)null }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.invalid.rowNumber", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_PartialAnswers_SavesDraftWithoutCompletenessCheck()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            new
            {
                questionCount = 2,
                scoringMode = "equal",
                totalScore = 10,
                rows = new[]
                {
                    new { questionNumber = 1, correctAnswer = "A", score = (decimal?)null },
                    new { questionNumber = 2, correctAnswer = "", score = (decimal?)null }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var rows = document.RootElement.GetProperty("rows");
        Assert.Equal("", rows[1].GetProperty("correctAnswer").GetString());
    }

    [Fact]
    public async Task Put_ReadyTemplate_ReturnsNotEditableConflict()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            BuildRequest(questionCount: 2, scoringMode: "equal", totalScore: 10m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("templates.notEditable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Put_SpeakingTemplate_ReturnsNotApplicable()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.EnsureSpeakingDraftTemplateAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(
            client,
            $"/api/test-templates/{templateId}/answer-key",
            BuildRequest(questionCount: 2, scoringMode: "equal", totalScore: 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("answerKey.notApplicable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Get_CrossTeacher_ReturnsHiddenNotFound()
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
    public async Task Put_CrossTeacher_ReturnsHiddenNotFound()
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
            BuildRequest(questionCount: 2, scoringMode: "equal", totalScore: 10m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    private static object BuildRequest(
        int questionCount,
        string scoringMode,
        decimal? totalScore,
        decimal? perQuestionScore = null)
    {
        var answers = new[] { "A", "B", "C", "D" };
        var rows = Enumerable.Range(1, questionCount)
            .Select(number => new
            {
                questionNumber = number,
                correctAnswer = answers[(number - 1) % answers.Length],
                score = perQuestionScore
            })
            .ToArray();

        return new { questionCount, scoringMode, totalScore, rows };
    }
}
