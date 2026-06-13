namespace EnglishTestWeb.Api.Contracts.Results;

public sealed record TeacherAnswerRowDto(
    int QuestionNumber,
    string? StudentAnswer,
    string CorrectAnswer,
    bool? IsCorrect,
    decimal? Score);
