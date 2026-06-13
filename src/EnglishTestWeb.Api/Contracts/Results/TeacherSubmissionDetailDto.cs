namespace EnglishTestWeb.Api.Contracts.Results;

public sealed record TeacherSubmissionDetailDto(
    Guid Id,
    string StudentName,
    string ClassName,
    string TemplateTitle,
    string Skill,
    string Mode,
    string Status,
    decimal? AutoScore,
    DateTimeOffset? SubmittedAt,
    IReadOnlyList<TeacherAnswerRowDto> Answers);
