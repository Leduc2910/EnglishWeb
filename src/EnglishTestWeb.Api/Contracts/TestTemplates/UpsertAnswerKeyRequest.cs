namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record UpsertAnswerKeyRequest(
    int QuestionCount,
    string? ScoringMode,
    decimal? TotalScore,
    IReadOnlyList<AnswerKeyRowRequest>? Rows);

public sealed record AnswerKeyRowRequest(
    int QuestionNumber,
    string? CorrectAnswer,
    decimal? Score);
