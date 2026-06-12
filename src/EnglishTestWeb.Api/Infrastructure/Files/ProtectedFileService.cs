using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.Submissions;
using EnglishTestWeb.Api.Domain.TestTemplates;
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

    public async Task<ProtectedFileAccessResult> OpenForStudentWithSubmissionAsync(
        Guid fileId,
        string studentId,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sub = await dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.Id == submissionId && s.StudentId == studentId)
            .Select(s => new
            {
                HomeworkTemplateId = s.HomeworkAssignment != null ? (Guid?)s.HomeworkAssignment.TestTemplateId : null,
                SessionTemplateId = s.LiveExamSession != null ? (Guid?)s.LiveExamSession.TestTemplateId : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (sub is null)
            return new ProtectedFileAccessResult(false, null, "files.notFound");

        var templateId = sub.HomeworkTemplateId ?? sub.SessionTemplateId;
        if (templateId is null)
            return new ProtectedFileAccessResult(false, null, "files.notFound");

        var metadata = await dbContext.TestMaterials
            .AsNoTracking()
            .Where(m => m.IsActive && m.StoredFileId == fileId && m.TemplateId == templateId.Value)
            .Select(m => new
            {
                m.StoredFile!.StorageKey,
                m.StoredFile.ContentType,
                m.StoredFile.OriginalFileName,
                m.StoredFile.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (metadata is null
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
