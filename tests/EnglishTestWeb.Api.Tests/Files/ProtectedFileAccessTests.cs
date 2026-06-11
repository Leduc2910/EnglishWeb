using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.TestTemplates;

namespace EnglishTestWeb.Api.Tests.Files;

public sealed class ProtectedFileAccessTests
{
    [Fact]
    public async Task GetContent_Owner_ReturnsPdfBytes()
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
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-1.4", System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal("bytes", response.Headers.AcceptRanges?.ToString());
    }

    [Fact]
    public async Task GetContent_WithRange_ReturnsPartialContent()
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{fileId}/content");
        request.Headers.Range = new RangeHeaderValue(0, 7);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(8, bytes.Length);
        Assert.Equal("%PDF-1.4", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task GetContent_CrossTeacher_ReturnsHiddenNotFound()
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
            Classes.ClassesTestHelper.OtherTeacherEmail,
            Classes.ClassesTestHelper.OtherTeacherPassword);

        var response = await otherClient.GetAsync($"/api/files/{fileId}/content");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("files.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
