using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.LiveExams;

namespace EnglishTestWeb.Api.Domain.Submissions;

public sealed class Submission
{
    public Guid Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public Guid? HomeworkAssignmentId { get; set; }

    public Guid? LiveExamSessionId { get; set; }

    public Guid? AnswerKeyVersionId { get; set; }

    public string Status { get; set; } = SubmissionStatuses.Draft;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public HomeworkAssignment? HomeworkAssignment { get; set; }

    public LiveExamSession? LiveExamSession { get; set; }

    public ICollection<SubmissionAnswer> Answers { get; set; } = [];
}
