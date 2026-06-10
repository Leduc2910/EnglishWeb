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
}
