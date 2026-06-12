namespace EnglishTestWeb.Api.Domain.Submissions;

public sealed class SubmissionAnswer
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public int QuestionNumber { get; set; }

    public string? Answer { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Submission? Submission { get; set; }
}
