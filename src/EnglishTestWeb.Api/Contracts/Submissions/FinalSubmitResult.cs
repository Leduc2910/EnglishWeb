namespace EnglishTestWeb.Api.Contracts.Submissions;

public sealed record FinalSubmitResult(
    bool Success,
    string? ErrorCode,
    SubmissionResultDto? Result);
