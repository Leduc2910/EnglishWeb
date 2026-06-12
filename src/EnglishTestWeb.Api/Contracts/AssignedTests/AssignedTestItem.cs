namespace EnglishTestWeb.Api.Contracts.AssignedTests;

public sealed record AssignedTestItem(
    Guid Id,
    string Mode,
    string Title,
    string Skill,
    Guid ClassId,
    string ClassName,
    string Status,
    string StudentStatus,
    DateTimeOffset? DeadlineAt,
    int? TimeLimitMinutes,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);
