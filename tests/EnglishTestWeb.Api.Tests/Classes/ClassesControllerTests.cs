using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Classes;

public sealed class ClassesControllerTests
{
    [Fact]
    public async Task LookupByCode_WithValidCode_ReturnsMinimalPreview()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/classes/by-code/ENG7A");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("English 7A", document.RootElement.GetProperty("className").GetString());
        Assert.Equal("ENG7A", document.RootElement.GetProperty("classCode").GetString());
        Assert.False(document.RootElement.TryGetProperty("students", out _));
    }

    [Fact]
    public async Task LookupByCode_WithNormalizedInput_MatchesClass()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/classes/by-code/eng-7a");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LookupByCode_WithUnknownCode_ReturnsCodeNotFound()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/classes/by-code/ZZZZ9");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.codeNotFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task LookupByCode_WithInactiveClass_ReturnsCodeInactive()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        await ClassesTestHelper.SeedInactiveClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/classes/by-code/INACTIVE1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.codeInactive", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetTeacherClasses_WithAuthenticatedTeacher_ReturnsOwnedClasses()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.TeacherEmail, AuthTestHelper.TeacherPassword);

        var response = await client.GetAsync("/api/classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetClassDetail_WithOtherTeacher_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            ClassesTestHelper.OtherTeacherEmail,
            ClassesTestHelper.OtherTeacherPassword);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var classId = await dbContext.Classes
            .Where(entity => entity.ClassCode == ClassesTestHelper.ClassCode)
            .Select(entity => entity.Id)
            .FirstAsync();

        var response = await client.GetAsync($"/api/classes/{classId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
