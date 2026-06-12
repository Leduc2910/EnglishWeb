namespace EnglishTestWeb.Api.Contracts.Submissions;

public sealed record SubmissionResultDto(
    Guid SubmissionId,
    string Status,
    string Mode,
    string TemplateTitle,
    DateTimeOffset SubmittedAt,
    decimal? AutoScore,
    int QuestionCount,
    int CorrectCount);
