namespace EnglishTestWeb.Api.Contracts.HomeworkAssignments;

public sealed record CreateHomeworkAssignmentRequest(
    Guid TemplateId,
    Guid ClassId,
    DateTimeOffset DeadlineAt,
    int? TimeLimitMinutes);
