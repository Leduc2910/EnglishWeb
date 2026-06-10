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
        using var testRoot = CreateTestRoot();
        var repositoryRoot = Path.Combine(testRoot.Path, "repo");
        var apiRoot = Path.Combine(repositoryRoot, "src", "EnglishTestWeb.Api");
        var webRoot = Path.Combine(apiRoot, "wwwroot");

        Directory.CreateDirectory(webRoot);
        File.WriteAllText(Path.Combine(repositoryRoot, "global.json"), "{}");

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(null, apiRoot, webRoot));

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize("   ", apiRoot, webRoot));

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(Path.Combine(repositoryRoot, "runtime-files"), apiRoot, webRoot));

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(Path.Combine(webRoot, "runtime-files"), apiRoot, webRoot));
    }

    [Fact]
    public void ValidateAndNormalize_RejectsRootUnderContentRootWhenRepositoryMarkerMissing()
    {
        using var testRoot = CreateTestRoot();
        var apiRoot = Path.Combine(testRoot.Path, "publish", "EnglishTestWeb.Api");
        var webRoot = Path.Combine(apiRoot, "wwwroot");

        Directory.CreateDirectory(webRoot);

        Assert.Throws<InvalidOperationException>(() =>
            ProtectedStoragePathValidator.ValidateAndNormalize(Path.Combine(apiRoot, "runtime-files"), apiRoot, webRoot));
    }

    [Fact]
    public async Task WriteAsync_StoresOpaqueFileOutsideWwwroot()
    {
        using var testRoot = CreateTestRoot();
        var repositoryRoot = Path.Combine(testRoot.Path, "repo");
        var apiRoot = Path.Combine(repositoryRoot, "src", "EnglishTestWeb.Api");
        var webRoot = Path.Combine(apiRoot, "wwwroot");
        var protectedRoot = Path.Combine(testRoot.Path, "protected-storage");

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
        Assert.Equal("storage smoke", await File.ReadAllTextAsync(Path.Combine(protectedRoot, result.StorageKey)));
    }

    [Fact]
    public async Task WriteAsync_WithNullStream_ThrowsArgumentNullException()
    {
        using var testRoot = CreateTestRoot();
        var apiRoot = Path.Combine(testRoot.Path, "api");
        var protectedRoot = Path.Combine(testRoot.Path, "protected-storage");
        Directory.CreateDirectory(apiRoot);

        var storage = new LocalProtectedFileStorage(
            Options.Create(new ProtectedStorageOptions { RootPath = protectedRoot }),
            new TestWebHostEnvironment(apiRoot, Path.Combine(apiRoot, "wwwroot")));

        await Assert.ThrowsAsync<ArgumentNullException>(() => storage.WriteAsync(null!));
    }

    [Fact]
    public async Task WriteAsync_WhenStreamExceedsMaxWriteBytes_ThrowsInvalidOperationException()
    {
        using var testRoot = CreateTestRoot();
        var apiRoot = Path.Combine(testRoot.Path, "api");
        var protectedRoot = Path.Combine(testRoot.Path, "protected-storage");
        Directory.CreateDirectory(apiRoot);

        var storage = new LocalProtectedFileStorage(
            Options.Create(new ProtectedStorageOptions { RootPath = protectedRoot, MaxWriteBytes = 1 }),
            new TestWebHostEnvironment(apiRoot, Path.Combine(apiRoot, "wwwroot")));

        await using var content = new MemoryStream([1, 2]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.WriteAsync(content));
    }

    [Fact]
    public async Task WriteAsync_WhenNonSeekableStreamExceedsMaxWriteBytes_ThrowsInvalidOperationException()
    {
        using var testRoot = CreateTestRoot();
        var apiRoot = Path.Combine(testRoot.Path, "api");
        var protectedRoot = Path.Combine(testRoot.Path, "protected-storage");
        Directory.CreateDirectory(apiRoot);

        var storage = new LocalProtectedFileStorage(
            Options.Create(new ProtectedStorageOptions { RootPath = protectedRoot, MaxWriteBytes = 1 }),
            new TestWebHostEnvironment(apiRoot, Path.Combine(apiRoot, "wwwroot")));

        await using var content = new NonSeekableStream(new MemoryStream([1, 2]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.WriteAsync(content));
    }

    private static TestRoot CreateTestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "EnglishTestWeb.Api.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TestRoot(root);
    }

    private sealed class TestRoot(string path) : IDisposable
    {
        public string Path { get; } = path;

        public void Dispose()
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
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

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
