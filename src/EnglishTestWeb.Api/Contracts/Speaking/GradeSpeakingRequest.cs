namespace EnglishTestWeb.Api.Contracts.Speaking;

public sealed record GradeSpeakingRequest(
    int Score,
    string? Feedback);
