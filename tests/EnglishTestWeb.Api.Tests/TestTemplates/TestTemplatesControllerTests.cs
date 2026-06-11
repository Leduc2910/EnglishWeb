using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

public sealed class TestTemplatesControllerTests
{
    [Fact]
    public async Task ListForTeacher_WithAuthenticatedTeacher_ReturnsOwnedTemplates()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/test-templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() >= 3);
    }

    [Fact]
    public async Task ListForTeacher_WithSkillFilter_ReturnsMatchingTemplates()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/test-templates?skill=listening");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(1, document.RootElement.GetArrayLength());
        Assert.Equal("listening", document.RootElement[0].GetProperty("skill").GetString());
    }

    [Fact]
    public async Task ListForTeacher_WithStatusFilter_ReturnsMatchingTemplates()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/test-templates?status=archived");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(1, document.RootElement.GetArrayLength());
        Assert.Equal("archived", document.RootElement[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListForTeacher_WithSearchQuery_ReturnsMatchingTemplates()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/test-templates?q=Listening");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(1, document.RootElement.GetArrayLength());
        Assert.Contains("Listening", document.RootElement[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListForTeacher_WithNoMatches_ReturnsEmptyArray()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/test-templates?q=nonexistent-template-xyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task ListForTeacher_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test-templates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task ListForTeacher_WithStudentRole_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.StudentEmail, AuthTestHelper.StudentPassword);

        var response = await client.GetAsync("/api/test-templates");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithOwnedTemplate_ReturnsDetail()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(TestTemplatesTestHelper.ReadyTitle, document.RootElement.GetProperty("title").GetString());
        Assert.Equal("ready", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetById_WithOtherTeacherTemplate_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            Classes.ClassesTestHelper.OtherTeacherEmail,
            Classes.ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithStudentRole_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.StudentEmail, AuthTestHelper.StudentPassword);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var templateId = await dbContext.TestTemplates
            .Select(entity => entity.Id)
            .FirstAsync();

        var response = await client.GetAsync($"/api/test-templates/{templateId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithValidDraft_ReturnsCreated()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "New Reading Draft",
            skill = "reading",
            description = "Demo description",
            tags = new[] { "midterm", "grade-10" }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("draft", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("reading", document.RootElement.GetProperty("skill").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsNameRequired()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.nameRequired", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithTooShortTitle_ReturnsNameRequired()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "ab",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.nameRequired", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithTitleTooLong_ReturnsTitleTooLong()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = new string('a', 121),
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.titleTooLong", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithDescriptionTooLong_ReturnsDescriptionTooLong()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Valid Title",
            skill = "reading",
            description = new string('d', 2001)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.descriptionTooLong", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithTagsJsonStorageOverflow_ReturnsTagsStorageLimit()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var tags = Enumerable.Range(1, 10)
            .Select(index => $"{new string('\\', 30)}{index}")
            .ToArray();

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Valid Title",
            skill = "reading",
            tags
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.tagsStorageLimit", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithTagTooLong_ReturnsTagTooLong()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Valid Title",
            skill = "reading",
            tags = new[] { new string('t', 33) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.tagTooLong", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithInvalidSkill_ReturnsSkillInvalid()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Valid Title",
            skill = "writing"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.skillInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithTooManyTags_ReturnsTagLimit()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/test-templates", new
        {
            title = "Valid Title",
            skill = "reading",
            tags = Enumerable.Range(1, 11).Select(index => $"tag-{index}").ToArray()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("templates.tagLimit", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Update_WithOwnedDraft_ReturnsOk()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/test-templates/{templateId}", new
        {
            title = "Updated Draft Title",
            skill = "listening",
            description = "Updated note",
            tags = new[] { "quiz" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("Updated Draft Title", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("listening", document.RootElement.GetProperty("skill").GetString());
    }

    [Fact]
    public async Task Update_WithOtherTeacherTemplate_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            Classes.ClassesTestHelper.OtherTeacherEmail,
            Classes.ClassesTestHelper.OtherTeacherPassword);

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
    public async Task Update_WithReadyTemplate_ReturnsNotEditable()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/test-templates/{templateId}", new
        {
            title = "Should Not Update",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("templates.notEditable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
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
    public async Task Create_WithStudentRole_ReturnsForbidden()
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
    public async Task Update_WithStudentRole_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await AuthTestHelper.PutJsonAsync(client, $"/api/test-templates/{templateId}", new
        {
            title = "Student Update",
            skill = "reading"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
