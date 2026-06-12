using EnglishTestWeb.Api.Domain.TestTemplates;

namespace EnglishTestWeb.Api.Domain.Assignments;

public sealed class HomeworkAssignment
{
    public Guid Id { get; set; }

    public string TeacherId { get; set; } = string.Empty;

    public Guid TestTemplateId { get; set; }

    public Guid ClassId { get; set; }

    public string Status { get; set; } = HomeworkAssignmentStatuses.Published;

    public DateTimeOffset DeadlineAt { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public TestTemplate? Template { get; set; }
}
