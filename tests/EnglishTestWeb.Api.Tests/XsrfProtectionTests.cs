using System.Net;
using System.Text;
using System.Text.Json;

namespace EnglishTestWeb.Api.Tests;

public sealed class XsrfProtectionTests
{
    [Fact]
    public async Task UnsafeApiRequestWithoutXsrfHeader_ReturnsProblemDetailsCode()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/health/unsafe-smoke",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal("auth.xsrfRequired", document.RootElement.GetProperty("code").GetString());
    }
}
