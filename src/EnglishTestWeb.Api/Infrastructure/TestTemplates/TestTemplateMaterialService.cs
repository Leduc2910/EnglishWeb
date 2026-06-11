using System.Security.Cryptography;
using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.TestTemplates;

public sealed class TestTemplateMaterialService(
    EnglishTestWebDbContext dbContext,
    ITemplateAuthorizationService templateAuthorizationService,
    IFileStorage fileStorage) : ITestTemplateMaterialService
{
    public async Task<TestMaterialAccessResult> ListMaterialsAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templateResult = await RequireViewableTemplateContextAsync(templateId, teacherId, cancellationToken);
        if (!templateResult.Allowed)
        {
            return new TestMaterialAccessResult(
                false,
                null,
                templateResult.ErrorCode,
                templateResult.StatusCode);
        }

        var items = await dbContext.TestMaterials
            .AsNoTracking()
            .Where(material => material.TemplateId == templateId && material.IsActive)
            .Include(material => material.StoredFile)
            .OrderBy(material => material.Role)
            .Select(material => MapResponse(material))
            .ToListAsync(cancellationToken);

        return new TestMaterialAccessResult(true, new TestMaterialListResponse(items), null);
    }

    public async Task<TestMaterialMutationResult> UploadMaterialAsync(
        Guid templateId,
        string teacherId,
        string role,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(content);

        var templateResult = await RequireEditableTemplateContextAsync(templateId, teacherId, cancellationToken);
        if (!templateResult.Allowed || templateResult.Template is null)
        {
            return new TestMaterialMutationResult(
                false,
                null,
                templateResult.ErrorCode,
                templateResult.StatusCode);
        }

        var roleError = MaterialUploadValidation.ValidateRoleForSkill(templateResult.Template.Skill, role);
        if (roleError is not null)
        {
            return new TestMaterialMutationResult(false, null, roleError, StatusCodes.Status400BadRequest);
        }

        if (content.CanSeek)
        {
            var sizeError = MaterialUploadValidation.ValidateFile(role, fileName, contentType, content.Length);
            if (sizeError is not null)
            {
                return new TestMaterialMutationResult(false, null, sizeError, StatusCodes.Status400BadRequest);
            }

            content.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return new TestMaterialMutationResult(
                false,
                null,
                "materials.roleInvalid",
                StatusCodes.Status400BadRequest);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        string? writtenStorageKey = null;
        try
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            var now = DateTimeOffset.UtcNow;
            var existingMaterials = await dbContext.TestMaterials
                .Where(material => material.TemplateId == templateId
                    && material.Role == normalizedRole
                    && material.IsActive)
                .Include(material => material.StoredFile)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingMaterials)
            {
                existing.IsActive = false;
                existing.ArchivedAt = now;
                if (existing.StoredFile is not null)
                {
                    existing.StoredFile.Status = StoredFileStatuses.Archived;
                    existing.StoredFile.UpdatedAt = now;
                }
            }

            string? checksum;
            FileStorageResult storageResult;
            using (var sha = SHA256.Create())
            await using (var hashingStream = new CryptoStream(content, sha, CryptoStreamMode.Read))
            {
                try
                {
                    storageResult = await fileStorage.WriteAsync(hashingStream, cancellationToken);
                    writtenStorageKey = storageResult.StorageKey;
                }
                catch (InvalidOperationException)
                {
                    return new TestMaterialMutationResult(
                        false,
                        null,
                        "files.tooLarge",
                        StatusCodes.Status400BadRequest);
                }
                catch (Exception)
                {
                    return new TestMaterialMutationResult(
                        false,
                        null,
                        "materials.uploadFailed",
                        StatusCodes.Status500InternalServerError);
                }

                checksum = Convert.ToHexString(sha.Hash!);
            }

            var fileValidationError = MaterialUploadValidation.ValidateFile(
                role,
                fileName,
                contentType,
                storageResult.Length);
            if (fileValidationError is not null)
            {
                await TryDeletePhysicalAsync(storageResult.StorageKey, cancellationToken);
                return new TestMaterialMutationResult(false, null, fileValidationError, StatusCodes.Status400BadRequest);
            }

            var storedFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = storageResult.StorageKey,
                OriginalFileName = Path.GetFileName(fileName),
                ContentType = contentType.Trim(),
                SizeBytes = storageResult.Length,
                ChecksumSha256 = checksum,
                OwnerUserId = teacherId,
                Status = StoredFileStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            var material = new TestMaterial
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                StoredFileId = storedFile.Id,
                Role = normalizedRole,
                IsActive = true,
                CreatedAt = now
            };

            templateResult.Template.UpdatedAt = now;
            dbContext.StoredFiles.Add(storedFile);
            dbContext.TestMaterials.Add(material);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new TestMaterialMutationResult(
                true,
                MapResponse(material, storedFile),
                null,
                StatusCodes.Status201Created);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (writtenStorageKey is not null)
            {
                await TryDeletePhysicalAsync(writtenStorageKey, cancellationToken);
            }

            return new TestMaterialMutationResult(
                false,
                null,
                "materials.uploadFailed",
                StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            if (writtenStorageKey is not null)
            {
                await TryDeletePhysicalAsync(writtenStorageKey, cancellationToken);
            }

            throw;
        }
    }

    public async Task<TestMaterialMutationResult> RemoveMaterialAsync(
        Guid templateId,
        string teacherId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templateResult = await RequireEditableTemplateContextAsync(templateId, teacherId, cancellationToken);
        if (!templateResult.Allowed)
        {
            return new TestMaterialMutationResult(
                false,
                null,
                templateResult.ErrorCode,
                templateResult.StatusCode);
        }

        var material = await dbContext.TestMaterials
            .Include(entity => entity.StoredFile)
            .FirstOrDefaultAsync(
                entity => entity.Id == materialId
                    && entity.TemplateId == templateId
                    && entity.IsActive,
                cancellationToken);

        if (material is null)
        {
            return new TestMaterialMutationResult(
                false,
                null,
                "materials.notFound",
                StatusCodes.Status404NotFound);
        }

        var now = DateTimeOffset.UtcNow;
        material.IsActive = false;
        material.ArchivedAt = now;
        if (material.StoredFile is not null)
        {
            material.StoredFile.Status = StoredFileStatuses.Archived;
            material.StoredFile.UpdatedAt = now;
        }

        if (templateResult.Template is not null)
        {
            templateResult.Template.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new TestMaterialMutationResult(true, null, null, StatusCodes.Status204NoContent);
    }

    private async Task<TemplateEditContextResult> RequireViewableTemplateContextAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        if (!decision.IsAllowed)
        {
            return new TemplateEditContextResult(
                false,
                null,
                decision.ErrorCode ?? "templates.notFound",
                StatusCodes.Status404NotFound);
        }

        return new TemplateEditContextResult(true, null, null, StatusCodes.Status200OK);
    }

    private async Task<TemplateEditContextResult> RequireEditableTemplateContextAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        if (!decision.IsAllowed)
        {
            return new TemplateEditContextResult(
                false,
                null,
                decision.ErrorCode ?? "templates.notFound",
                StatusCodes.Status404NotFound);
        }

        var template = await dbContext.TestTemplates
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null)
        {
            return new TemplateEditContextResult(false, null, "templates.notFound", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(template.Status, TemplateStatuses.Draft, StringComparison.Ordinal))
        {
            return new TemplateEditContextResult(
                false,
                template,
                "templates.notEditable",
                StatusCodes.Status409Conflict);
        }

        return new TemplateEditContextResult(true, template, null, StatusCodes.Status200OK);
    }

    private async Task TryDeletePhysicalAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static TestMaterialResponse MapResponse(TestMaterial material)
    {
        if (material.StoredFile is null)
        {
            throw new InvalidOperationException("Stored file metadata is required.");
        }

        return MapResponse(material, material.StoredFile);
    }

    private static TestMaterialResponse MapResponse(TestMaterial material, StoredFile storedFile) =>
        new(
            material.Id,
            storedFile.Id,
            material.Role,
            storedFile.OriginalFileName,
            storedFile.SizeBytes,
            storedFile.ContentType,
            material.CreatedAt);

    private sealed record TemplateEditContextResult(
        bool Allowed,
        TestTemplate? Template,
        string? ErrorCode,
        int StatusCode);
}
