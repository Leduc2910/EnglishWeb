namespace EnglishTestWeb.Api.Application.Files;

public interface IFileStorage
{
    Task<FileStorageResult> WriteAsync(Stream content, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
