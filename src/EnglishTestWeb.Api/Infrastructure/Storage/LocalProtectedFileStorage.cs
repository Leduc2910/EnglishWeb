using EnglishTestWeb.Api.Application.Files;
using Microsoft.Extensions.Options;

namespace EnglishTestWeb.Api.Infrastructure.Storage;

public sealed class LocalProtectedFileStorage(
    IOptions<ProtectedStorageOptions> options,
    IWebHostEnvironment environment) : IFileStorage
{
    private const long DefaultMaxWriteBytes = 100 * 1024 * 1024;

    public async Task<FileStorageResult> WriteAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var maxWriteBytes = options.Value.MaxWriteBytes ?? DefaultMaxWriteBytes;
        if (content.CanSeek && content.Length > maxWriteBytes)
        {
            throw new InvalidOperationException($"Protected storage writes are limited to {maxWriteBytes} bytes.");
        }

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

        var writtenBytes = await CopyWithLimitAsync(content, output, maxWriteBytes, cancellationToken);
        return new FileStorageResult(storageKey, writtenBytes);
    }

    private static async Task<long> CopyWithLimitAsync(
        Stream input,
        Stream output,
        long maxWriteBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        long totalBytes = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (totalBytes > maxWriteBytes)
            {
                throw new InvalidOperationException(
                    $"Protected storage writes are limited to {maxWriteBytes} bytes.");
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return totalBytes;
    }
}
