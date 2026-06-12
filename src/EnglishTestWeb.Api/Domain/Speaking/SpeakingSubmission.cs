using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.LiveExams;

namespace EnglishTestWeb.Api.Domain.Speaking;

public sealed class SpeakingSubmission
{
    public Guid Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public Guid? HomeworkAssignmentId { get; set; }

    public Guid? LiveExamSessionId { get; set; }

    public Guid? DraftStoredFileId { get; set; }

    public string Status { get; set; } = SpeakingSubmissionStatuses.Draft;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public HomeworkAssignment? HomeworkAssignment { get; set; }

    public LiveExamSession? LiveExamSession { get; set; }

    public StoredFile? DraftStoredFile { get; set; }
}
