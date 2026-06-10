using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EnglishTestWeb.Api.Infrastructure.Security;

namespace EnglishTestWeb.Api.Tests;

public sealed class XsrfProtectionTests
{
    private const string UnsafeSmokePath = "/api/health/unsafe-smoke";

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task UnsafeApiRequestWithoutXsrfHeader_ReturnsProblemDetailsCode(string method)
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var request = CreateUnsafeRequest(new HttpMethod(method), UnsafeSmokePath, includeInvalidToken: false);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("auth.xsrfRequired", await ReadProblemDetailsCodeAsync(response));
    }

    [Fact]
    public async Task UnsafeApiRequestWithInvalidXsrfHeader_ReturnsInvalidProblemDetailsCode()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var request = CreateUnsafeRequest(HttpMethod.Post, UnsafeSmokePath, includeInvalidToken: true);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("auth.xsrfInvalid", await ReadProblemDetailsCodeAsync(response));
    }

    private static HttpRequestMessage CreateUnsafeRequest(HttpMethod method, string path, bool includeInvalidToken)
    {
        var request = new HttpRequestMessage(method, path);
        if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        if (includeInvalidToken)
        {
            request.Headers.Add(XsrfDefaults.HeaderName, "invalid-token");
        }

        return request;
    }

    private static async Task<string?> ReadProblemDetailsCodeAsync(HttpResponseMessage response)
    {
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.True(document.RootElement.TryGetProperty("code", out var codeElement));
        return codeElement.GetString();
    }
}
