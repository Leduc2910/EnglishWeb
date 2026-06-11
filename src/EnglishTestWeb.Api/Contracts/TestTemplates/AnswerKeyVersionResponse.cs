namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record AnswerKeyVersionResponse(
    Guid AnswerKeyVersionId,
    Guid TemplateId,
    string Status,
    string ScoringMode,
    int QuestionCount,
    decimal? TotalScore,
    IReadOnlyList<AnswerKeyRowResponse> Rows,
    DateTimeOffset UpdatedAt);

public sealed record AnswerKeyRowResponse(
    int QuestionNumber,
    string CorrectAnswer,
    decimal? Score);
