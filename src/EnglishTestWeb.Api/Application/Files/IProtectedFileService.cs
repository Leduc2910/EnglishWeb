namespace EnglishTestWeb.Api.Application.Files;

public interface IProtectedFileService
{
    Task<ProtectedFileAccessResult> OpenForAuthorizedUserAsync(
        Guid fileId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProtectedFileAccessResult> OpenForStudentWithSubmissionAsync(
        Guid fileId,
        string studentId,
        Guid submissionId,
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
