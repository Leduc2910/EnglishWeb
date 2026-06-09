using EnglishTestWeb.Api.Application.Files;
using Microsoft.Extensions.Options;

namespace EnglishTestWeb.Api.Infrastructure.Storage;

public sealed class LocalProtectedFileStorage(
    IOptions<ProtectedStorageOptions> options,
    IWebHostEnvironment environment) : IFileStorage
{
    public async Task<FileStorageResult> WriteAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var rootPath = ProtectedStoragePathValidator.ValidateAndNormalize(
            options.Value.RootPath,
            environment.ContentRootPath,
            environment.WebRootPath);

        Directory.CreateDirectory(rootPath);

        var storageKey = Guid.NewGuid().ToString("N");
        var targetPath = Path.Combine(rootPath, storageKey);

        await using var output = new FileStream(
            targetPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
        return new FileStorageResult(storageKey, output.Length);
    }
}
