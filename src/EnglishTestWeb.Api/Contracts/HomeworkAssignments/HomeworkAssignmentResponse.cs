namespace EnglishTestWeb.Api.Contracts.HomeworkAssignments;

public sealed record HomeworkAssignmentResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateTitle,
    string TemplateSkill,
    Guid ClassId,
    string ClassName,
    DateTimeOffset DeadlineAt,
    int? TimeLimitMinutes,
    string Status,
    string Mode,
    IReadOnlyList<string> AllowedActions,
    DateTimeOffset CreatedAt);
