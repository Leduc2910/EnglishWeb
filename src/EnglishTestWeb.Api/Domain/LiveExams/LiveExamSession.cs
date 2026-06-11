namespace EnglishTestWeb.Api.Domain.LiveExams;

public sealed class LiveExamSession
{
    public Guid Id { get; set; }

    public string TeacherId { get; set; } = string.Empty;

    public Guid TestTemplateId { get; set; }

    public Guid ClassId { get; set; }

    public string Status { get; set; } = LiveExamSessionStatuses.Scheduled;

    public DateTimeOffset? ScheduledStartAt { get; set; }

    public DateTimeOffset? ScheduledEndAt { get; set; }

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
