namespace EnglishTestWeb.Api.Contracts.Speaking;

public sealed record CreateSpeakingSubmissionRequest(
    Guid? HomeworkAssignmentId,
    Guid? LiveExamSessionId);
