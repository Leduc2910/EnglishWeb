using System.Text;
using EnglishTestWeb.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishTestWeb.Api.Tests;

public sealed class ProtectedStorageTests
{
    [Fact]
    public void ValidateAndNormalize_RejectsMissingRepositoryAndWwwrootRoots()
    {
        var testRoot = CreateTestRoot();
        var repositoryRoot = Path.Combine(testRoot, "repo");
        var apiRoot = Path.Combine(repositoryRoot, "src", "EnglishTestWeb.Api");
        var webRoot = Path.Combine(apiRoot, "wwwroot");

        Directory.CreateDirectory(webRoot);
        File.WriteAllText(Path.Combine(repositoryRoot, "global.json"), "{}");

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(null, apiRoot, webRoot));

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(Path.Combine(repositoryRoot, "runtime-files"), apiRoot, webRoot));

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(Path.Combine(webRoot, "runtime-files"), apiRoot, webRoot));
    }

    [Fact]
    public async Task WriteAsync_StoresOpaqueFileOutsideWwwroot()
    {
        var testRoot = CreateTestRoot();
        var repositoryRoot = Path.Combine(testRoot, "repo");
        var apiRoot = Path.Combine(repositoryRoot, "src", "EnglishTestWeb.Api");
        var webRoot = Path.Combine(apiRoot, "wwwroot");
        var protectedRoot = Path.Combine(testRoot, "protected-storage");

        Directory.CreateDirectory(webRoot);
        File.WriteAllText(Path.Combine(repositoryRoot, "global.json"), "{}");

        var storage = new LocalProtectedFileStorage(
            Options.Create(new ProtectedStorageOptions { RootPath = protectedRoot }),
            new TestWebHostEnvironment(apiRoot, webRoot));

        var payload = Encoding.UTF8.GetBytes("storage smoke");
        await using var content = new MemoryStream(payload);
        var result = await storage.WriteAsync(content);

        Assert.Equal(payload.Length, result.Length);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, result.StorageKey);
        Assert.DoesNotContain(Path.AltDirectorySeparatorChar, result.StorageKey);
        Assert.True(File.Exists(Path.Combine(protectedRoot, result.StorageKey)));
        Assert.False(File.Exists(Path.Combine(webRoot, result.StorageKey)));
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "EnglishTestWeb.Api.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class TestWebHostEnvironment(string contentRootPath, string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "EnglishTestWeb.Api.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = webRootPath;

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ContentRootPath { get; set; } = contentRootPath;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
