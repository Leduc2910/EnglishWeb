namespace EnglishTestWeb.Api.Application.Files;

public interface IProtectedFileService
{
    Task<ProtectedFileAccessResult> OpenForAuthorizedUserAsync(
        Guid fileId,
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed record ProtectedFileAccessResult(
    bool Allowed,
    ProtectedFileStream? File,
    string? ErrorCode);

public sealed record ProtectedFileStream(
    Stream Content,
    string ContentType,
    string OriginalFileName);
