namespace EnglishTestWeb.Api.Contracts.LiveExamSessions;

public sealed record CreateLiveExamSessionRequest(
    Guid TemplateId,
    Guid ClassId,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt);
