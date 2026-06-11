using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Files;

public sealed class ProtectedFileService(
    EnglishTestWebDbContext dbContext,
    IFileStorage fileStorage) : IProtectedFileService
{
    public async Task<ProtectedFileAccessResult> OpenForAuthorizedUserAsync(
        Guid fileId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = await dbContext.TestMaterials
            .AsNoTracking()
            .Where(material => material.IsActive && material.StoredFileId == fileId)
            .Select(material => new
            {
                material.StoredFile!.StorageKey,
                material.StoredFile.ContentType,
                material.StoredFile.OriginalFileName,
                material.StoredFile.Status,
                material.Template!.TeacherId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (metadata is null
            || !string.Equals(metadata.TeacherId, userId, StringComparison.Ordinal)
            || !string.Equals(metadata.Status, StoredFileStatuses.Active, StringComparison.Ordinal))
        {
            return new ProtectedFileAccessResult(false, null, "files.notFound");
        }

        try
        {
            var stream = await fileStorage.OpenReadAsync(metadata.StorageKey, cancellationToken);
            return new ProtectedFileAccessResult(
                true,
                new ProtectedFileStream(stream, metadata.ContentType, metadata.OriginalFileName),
                null);
        }
        catch (FileNotFoundException)
        {
            return new ProtectedFileAccessResult(false, null, "files.notFound");
        }
    }
}
