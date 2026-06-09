namespace EnglishTestWeb.Api.Application.Files;

public interface IFileStorage
{
    Task<FileStorageResult> WriteAsync(Stream content, CancellationToken cancellationToken = default);
}
