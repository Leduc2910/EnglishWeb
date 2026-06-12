namespace EnglishTestWeb.Api.Contracts.Submissions;

public sealed record SubmissionWorkspaceDto(
    Guid Id,
    string Status,
    string Mode,
    string TemplateTitle,
    string Skill,
    Guid ClassId,
    string ClassName,
    Guid? HomeworkAssignmentId,
    Guid? LiveExamSessionId,
    DateTimeOffset? DeadlineAt,
    int? TimeLimitMinutes,
    DateTimeOffset? SessionOpenedAt,
    DateTimeOffset? SessionClosedAt,
    Guid PdfMaterialId,
    Guid? AudioMaterialId,
    int QuestionCount,
    IReadOnlyList<AnswerRowDto> AnswerRows);
