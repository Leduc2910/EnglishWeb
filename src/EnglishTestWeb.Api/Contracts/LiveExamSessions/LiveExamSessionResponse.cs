namespace EnglishTestWeb.Api.Contracts.LiveExamSessions;

public sealed record LiveExamSessionResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateTitle,
    string TemplateSkill,
    Guid ClassId,
    string ClassName,
    string Status,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);
