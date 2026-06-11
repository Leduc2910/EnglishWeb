using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

public sealed class TestTemplateMaterialsControllerTests
{
    [Fact]
    public async Task UploadPdf_WithDraftTemplate_ReturnsCreated()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("pdf", document.RootElement.GetProperty("role").GetString());
        Assert.True(document.RootElement.GetProperty("fileId").GetGuid() != Guid.Empty);
    }

    [Fact]
    public async Task Upload_WithInvalidMime_ReturnsInvalidType()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadInvalidTypeAsync(client, templateId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("files.invalidType", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Upload_ReadyTemplate_ReturnsNotEditable()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("templates.notEditable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Upload_CrossTeacher_ReturnsHiddenNotFound()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInUserAsync(
            client,
            Classes.ClassesTestHelper.OtherTeacherEmail,
            Classes.ClassesTestHelper.OtherTeacherPassword);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("templates.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Upload_ReplaceSameRole_ArchivesPreviousActiveMaterial()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var first = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId, fileName: "first.pdf");
        first.EnsureSuccessStatusCode();

        var second = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId, fileName: "second.pdf");
        second.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var activeCount = await dbContext.TestMaterials
            .CountAsync(material => material.TemplateId == templateId && material.Role == "pdf" && material.IsActive);
        var archivedCount = await dbContext.TestMaterials
            .CountAsync(material => material.TemplateId == templateId && material.Role == "pdf" && !material.IsActive);

        Assert.Equal(1, activeCount);
        Assert.Equal(1, archivedCount);
    }

    [Fact]
    public async Task ListMaterials_ReturnsActiveItems()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        (await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId)).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/test-templates/{templateId}/materials");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(1, document.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Upload_WithStudent_ReturnsForbidden()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentAsync(client);

        var templateId = await TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var response = await TestTemplateMaterialsTestHelper.UploadPdfAsync(client, templateId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
