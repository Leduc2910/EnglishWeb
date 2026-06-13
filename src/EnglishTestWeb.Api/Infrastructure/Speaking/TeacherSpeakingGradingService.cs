using EnglishTestWeb.Api.Application.Speaking;
using EnglishTestWeb.Api.Contracts.Speaking;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Speaking;

public sealed class TeacherSpeakingGradingService(
    EnglishTestWebDbContext db,
    TimeProvider timeProvider) : ITeacherSpeakingGradingService
{
    public async Task<(bool Success, string? ErrorCode, TeacherSpeakingSubmissionDto? Dto)> GetForTeacherAsync(
        Guid speakingSubmissionId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
            .Include(s => s.LiveExamSession).ThenInclude(s => s!.Template)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == speakingSubmissionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "speaking.notFound", null);

        // Scope check: teacher must own the assignment/session
        var sourceTeacherId = submission.HomeworkAssignment?.TeacherId
                           ?? submission.LiveExamSession?.TeacherId;
        if (sourceTeacherId != teacherId)
            return (false, "speaking.notFound", null);

        var templateTitle = submission.HomeworkAssignment?.Template?.Title
                         ?? submission.LiveExamSession?.Template?.Title
                         ?? string.Empty;

        var dto = await BuildDtoAsync(submission, templateTitle, cancellationToken);
        return (true, null, dto);
    }

    public async Task<(bool Success, string? ErrorCode, TeacherSpeakingSubmissionDto? Dto)> GradeAsync(
        Guid speakingSubmissionId,
        string teacherId,
        GradeSpeakingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Score < 0 || request.Score > 10)
            return (false, "speaking.scoreInvalid", null);

        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment).ThenInclude(h => h!.Template)
            .Include(s => s.LiveExamSession).ThenInclude(s => s!.Template)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == speakingSubmissionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return (false, "speaking.notFound", null);

        var sourceTeacherId = submission.HomeworkAssignment?.TeacherId
                           ?? submission.LiveExamSession?.TeacherId;
        if (sourceTeacherId != teacherId)
            return (false, "speaking.notFound", null);

        if (submission.Status == SpeakingSubmissionStatuses.Draft)
            return (false, "speaking.notSubmitted", null);

        var now = timeProvider.GetUtcNow();
        submission.Score = request.Score;
        submission.Feedback = request.Feedback?.Trim();
        submission.GraderId = teacherId;
        submission.GradedAt = now;
        submission.Status = SpeakingSubmissionStatuses.Graded;
        submission.UpdatedAt = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Last-write-wins: reload server values and retry once
            foreach (var entry in ex.Entries)
                await entry.ReloadAsync(cancellationToken);
            submission.Score = request.Score;
            submission.Feedback = request.Feedback?.Trim();
            submission.GraderId = teacherId;
            submission.GradedAt = now;
            submission.Status = SpeakingSubmissionStatuses.Graded;
            submission.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        var templateTitle = submission.HomeworkAssignment?.Template?.Title
                         ?? submission.LiveExamSession?.Template?.Title
                         ?? string.Empty;
        var dto = await BuildDtoAsync(submission, templateTitle, cancellationToken);
        return (true, null, dto);
    }

    private async Task<TeacherSpeakingSubmissionDto> BuildDtoAsync(
        SpeakingSubmission submission,
        string templateTitle,
        CancellationToken cancellationToken)
    {
        var studentName = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == submission.StudentId)
            .Select(u => u.UserName ?? u.Email ?? submission.StudentId)
            .FirstOrDefaultAsync(cancellationToken) ?? submission.StudentId;

        var classId = submission.HomeworkAssignment?.ClassId
                   ?? submission.LiveExamSession?.ClassId
                   ?? Guid.Empty;
        var className = await db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var mode = submission.HomeworkAssignmentId.HasValue ? "homework" : "live-exam";

        string? submittedFileId = null;
        string? submittedFileName = null;
        long? submittedFileSizeBytes = null;

        if (submission.DraftStoredFile is not null)
        {
            submittedFileId = submission.DraftStoredFile.Id.ToString();
            submittedFileName = submission.DraftStoredFile.OriginalFileName;
            submittedFileSizeBytes = submission.DraftStoredFile.SizeBytes;
        }
        else if (submission.DraftStoredFileId.HasValue)
        {
            var file = await db.StoredFiles
                .AsNoTracking()
                .Where(f => f.Id == submission.DraftStoredFileId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (file is not null)
            {
                submittedFileId = file.Id.ToString();
                submittedFileName = file.OriginalFileName;
                submittedFileSizeBytes = file.SizeBytes;
            }
        }

        return new TeacherSpeakingSubmissionDto(
            Id: submission.Id,
            StudentName: studentName,
            ClassName: className,
            TemplateTitle: templateTitle,
            Mode: mode,
            Status: submission.Status,
            SubmittedAt: submission.SubmittedAt,
            SubmittedFileName: submittedFileName,
            SubmittedFileSizeBytes: submittedFileSizeBytes,
            SubmittedFileId: submittedFileId,
            IsFileMissing: false,
            Score: submission.Score,
            Feedback: submission.Feedback,
            GraderId: submission.GraderId,
            GradedAt: submission.GradedAt);
    }
}
