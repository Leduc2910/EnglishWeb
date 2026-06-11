using System.Net.Http.Headers;
using System.Text;
using EnglishTestWeb.Api.Tests.Auth;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

internal static class TestTemplateMaterialsTestHelper
{
    internal static readonly byte[] MinimalPdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 minimal test content");

    internal static Task<HttpResponseMessage> UploadPdfAsync(
        HttpClient client,
        Guid templateId,
        string role = "pdf",
        string fileName = "sample.pdf") =>
        AuthTestHelper.PostMultipartAsync(client, $"/api/test-templates/{templateId}/materials", content =>
        {
            var fileContent = new ByteArrayContent(MinimalPdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(role), "role");
        });

    internal static Task<HttpResponseMessage> UploadInvalidTypeAsync(
        HttpClient client,
        Guid templateId,
        string role = "pdf") =>
        AuthTestHelper.PostMultipartAsync(client, $"/api/test-templates/{templateId}/materials", content =>
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-a-pdf"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(fileContent, "file", "notes.txt");
            content.Add(new StringContent(role), "role");
        });
}
