using System.Security.Cryptography;
using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Speaking;
using EnglishTestWeb.Api.Contracts.Speaking;
using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Speaking;

public sealed class SpeakingSubmissionService(
    EnglishTestWebDbContext db,
    IFileStorage fileStorage,
    TimeProvider timeProvider) : ISpeakingSubmissionService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
        "audio/webm",
        "audio/mp4",
        "video/mp4",
        "video/webm",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".webm", ".mp4", ".m4a", ".mpeg",
    };

    private const long MaxFileSizeBytes = 104_857_600; // 100MB

    public async Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> CreateOrResumeAsync(
        string studentId,
        Guid activeClassId,
        CreateSpeakingSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasHomework = request.HomeworkAssignmentId.HasValue;
        var hasSession = request.LiveExamSessionId.HasValue;

        if (hasHomework == hasSession)
            return (false, "speaking.invalidSource", null);

        var now = timeProvider.GetUtcNow();

        Guid templateId;
        Guid sourceClassId;
        bool isSourceOpen;

        if (hasHomework)
        {
            var homework = await db.HomeworkAssignments
                .AsNoTracking()
                .Where(h => h.Id == request.HomeworkAssignmentId!.Value)
                .Select(h => new { h.ClassId, h.DeadlineAt, h.TestTemplateId })
                .FirstOrDefaultAsync(cancellationToken);

            if (homework is null || homework.ClassId != activeClassId)
                return (false, "speaking.sourceUnavailable", null);

            templateId = homework.TestTemplateId;
            sourceClassId = homework.ClassId;
            isSourceOpen = homework.DeadlineAt > now;
        }
        else
        {
            var session = await db.LiveExamSessions
                .AsNoTracking()
                .Where(s => s.Id == request.LiveExamSessionId!.Value)
                .Select(s => new { s.ClassId, s.Status, s.TestTemplateId })
                .FirstOrDefaultAsync(cancellationToken);

            if (session is null || session.ClassId != activeClassId)
                return (false, "speaking.sourceUnavailable", null);

            templateId = session.TestTemplateId;
            sourceClassId = session.ClassId;
            isSourceOpen = session.Status == LiveExamSessionStatuses.Open;
        }

        // Idempotent: return existing if already created
        SpeakingSubmission? existing;
        if (hasHomework)
        {
            existing = await db.SpeakingSubmissions
                .Include(s => s.DraftStoredFile)
                .Where(s => s.StudentId == studentId && s.HomeworkAssignmentId == request.HomeworkAssignmentId!.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            existing = await db.SpeakingSubmissions
                .Include(s => s.DraftStoredFile)
                .Where(s => s.StudentId == studentId && s.LiveExamSessionId == request.LiveExamSessionId!.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (existing is not null)
        {
            var existingDto = await BuildDtoAsync(existing, templateId, sourceClassId, isSourceOpen, cancellationToken);
            return (true, null, existingDto);
        }

        var submission = new SpeakingSubmission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = request.HomeworkAssignmentId,
            LiveExamSessionId = request.LiveExamSessionId,
            Status = SpeakingSubmissionStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.SpeakingSubmissions.Add(submission);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert hit unique index — re-query for the winner
            db.Entry(submission).State = EntityState.Detached;
            SpeakingSubmission? raceWinner = hasHomework
                ? await db.SpeakingSubmissions
                    .Include(s => s.DraftStoredFile)
                    .Where(s => s.StudentId == studentId && s.HomeworkAssignmentId == request.HomeworkAssignmentId!.Value)
                    .FirstOrDefaultAsync(cancellationToken)
                : await db.SpeakingSubmissions
                    .Include(s => s.DraftStoredFile)
                    .Where(s => s.StudentId == studentId && s.LiveExamSessionId == request.LiveExamSessionId!.Value)
                    .FirstOrDefaultAsync(cancellationToken);

            if (raceWinner is not null)
            {
                var winnerDto = await BuildDtoAsync(raceWinner, templateId, sourceClassId, isSourceOpen, cancellationToken);
                return (true, null, winnerDto);
            }
            throw;
        }

        var dto = await BuildDtoAsync(submission, templateId, sourceClassId, isSourceOpen, cancellationToken);
        return (true, null, dto);
    }

    public async Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> GetAsync(
        Guid speakingSubmissionId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == speakingSubmissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "speaking.notFound", null);

        var (templateId, sourceClassId, isSourceOpen) = await GetSourceInfoAsync(submission, cancellationToken);
        var dto = await BuildDtoAsync(submission, templateId, sourceClassId, isSourceOpen, cancellationToken);
        return (true, null, dto);
    }

    public async Task<(bool Success, string? ErrorCode, SpeakingSubmissionDto? Dto)> UploadDraftAsync(
        Guid speakingSubmissionId,
        string studentId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == speakingSubmissionId && s.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "speaking.notFound", null);

        if (submission.Status != SpeakingSubmissionStatuses.Draft)
            return (false, "speaking.alreadySubmitted", null);

        // Enforce source is still open (deadline / session status re-checked from DB state)
        var (_, _, isSourceOpen) = await GetSourceInfoAsync(submission, cancellationToken);
        if (!isSourceOpen)
            return (false, "speaking.sourceUnavailable", null);

        // Validate MIME type and file extension (both must be in the allowed set)
        if (!AllowedMimeTypes.Contains(file.ContentType?.Trim() ?? string.Empty))
            return (false, "speaking.invalidFileType", null);

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return (false, "speaking.invalidFileType", null);

        if (file.Length > MaxFileSizeBytes)
            return (false, "speaking.fileTooLarge", null);

        var now = timeProvider.GetUtcNow();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        string? writtenStorageKey = null;
        try
        {
            // Archive old draft file if exists
            if (submission.DraftStoredFile is not null)
            {
                submission.DraftStoredFile.Status = StoredFileStatuses.Archived;
                submission.DraftStoredFile.UpdatedAt = now;
                submission.DraftStoredFileId = null;
            }

            // Upload new file
            string checksum;
            FileStorageResult storageResult;
            await using var stream = file.OpenReadStream();
            using (var sha = SHA256.Create())
            {
                await using (var hashingStream = new CryptoStream(stream, sha, CryptoStreamMode.Read))
                {
                    storageResult = await fileStorage.WriteAsync(hashingStream, cancellationToken);
                    writtenStorageKey = storageResult.StorageKey;
                }
                // CryptoStream disposed above — FlushFinalBlock called, hash is final
                checksum = Convert.ToHexString(sha.Hash!);
            }

            var newFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = storageResult.StorageKey,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType?.Trim() ?? string.Empty,
                SizeBytes = storageResult.Length,
                ChecksumSha256 = checksum,
                OwnerUserId = studentId,
                Status = StoredFileStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.StoredFiles.Add(newFile);
            submission.DraftStoredFileId = newFile.Id;
            submission.DraftStoredFile = newFile;
            submission.UpdatedAt = now;

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var (dtoTemplateId, dtoSourceClassId, dtoIsSourceOpen) = await GetSourceInfoAsync(submission, cancellationToken);
            var dto = await BuildDtoAsync(submission, dtoTemplateId, dtoSourceClassId, dtoIsSourceOpen, cancellationToken);
            return (true, null, dto);
        }
        catch (InvalidOperationException)
        {
            // Storage layer rejected the file (e.g. size exceeded its own limit)
            await transaction.RollbackAsync(CancellationToken.None);
            if (writtenStorageKey is not null)
                try { await fileStorage.DeleteAsync(writtenStorageKey, CancellationToken.None); } catch { }
            return (false, "speaking.fileTooLarge", null);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (writtenStorageKey is not null)
                try { await fileStorage.DeleteAsync(writtenStorageKey, CancellationToken.None); } catch { }
            throw;
        }
    }

    private async Task<(Guid TemplateId, Guid SourceClassId, bool IsSourceOpen)> GetSourceInfoAsync(
        SpeakingSubmission submission,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        if (submission.HomeworkAssignmentId.HasValue)
        {
            // HomeworkAssignment may already be loaded via Include
            HomeworkAssignment? hw = submission.HomeworkAssignment;
            if (hw is null)
            {
                hw = await db.HomeworkAssignments
                    .AsNoTracking()
                    .Where(h => h.Id == submission.HomeworkAssignmentId.Value)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (hw is null)
                throw new InvalidOperationException(
                    $"HomeworkAssignment {submission.HomeworkAssignmentId} not found for speaking submission {submission.Id}.");
            return (hw.TestTemplateId, hw.ClassId, hw.DeadlineAt > now);
        }
        else
        {
            LiveExamSession? session = submission.LiveExamSession;
            if (session is null)
            {
                session = await db.LiveExamSessions
                    .AsNoTracking()
                    .Where(s => s.Id == submission.LiveExamSessionId!.Value)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (session is null)
                throw new InvalidOperationException(
                    $"LiveExamSession {submission.LiveExamSessionId} not found for speaking submission {submission.Id}.");
            return (session.TestTemplateId, session.ClassId, session.Status == LiveExamSessionStatuses.Open);
        }
    }

    private async Task<SpeakingSubmissionDto> BuildDtoAsync(
        SpeakingSubmission submission,
        Guid templateId,
        Guid sourceClassId,
        bool isSourceOpen,
        CancellationToken cancellationToken)
    {
        var templateInfo = await db.TestTemplates
            .AsNoTracking()
            .Where(t => t.Id == templateId)
            .Select(t => new { t.Title, t.Skill })
            .FirstOrDefaultAsync(cancellationToken);

        var className = await db.Classes
            .AsNoTracking()
            .Where(c => c.Id == sourceClassId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var cueMaterial = await db.TestMaterials
            .AsNoTracking()
            .Include(m => m.StoredFile)
            .Where(m => m.TemplateId == templateId && m.Role == MaterialRoles.Cue && m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var mode = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";

        DraftFileDto? draftFile = null;
        if (submission.DraftStoredFile is not null)
        {
            draftFile = new DraftFileDto(
                submission.DraftStoredFile.Id,
                submission.DraftStoredFile.OriginalFileName,
                submission.DraftStoredFile.SizeBytes,
                submission.DraftStoredFile.CreatedAt);
        }
        else if (submission.DraftStoredFileId.HasValue)
        {
            // Load if not included
            var file = await db.StoredFiles
                .AsNoTracking()
                .Where(f => f.Id == submission.DraftStoredFileId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (file is not null && file.Status == StoredFileStatuses.Active)
            {
                draftFile = new DraftFileDto(file.Id, file.OriginalFileName, file.SizeBytes, file.CreatedAt);
            }
        }

        return new SpeakingSubmissionDto(
            Id: submission.Id,
            Status: submission.Status,
            Mode: mode,
            TemplateTitle: templateInfo?.Title ?? string.Empty,
            TemplateSkill: templateInfo?.Skill ?? string.Empty,
            ClassName: className,
            IsSourceOpen: isSourceOpen,
            CueMaterialFileId: cueMaterial?.StoredFileId.ToString(),
            CueMaterialFileName: cueMaterial?.StoredFile?.OriginalFileName,
            DraftFile: draftFile);
    }
}
