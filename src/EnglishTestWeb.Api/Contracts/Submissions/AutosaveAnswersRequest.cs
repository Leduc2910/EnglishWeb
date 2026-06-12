namespace EnglishTestWeb.Api.Contracts.Submissions;

public sealed record AutosaveAnswersRequest(IReadOnlyList<AnswerRowDto> Rows);
