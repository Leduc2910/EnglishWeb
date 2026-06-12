namespace EnglishTestWeb.Api.Contracts.Submissions;

public sealed record CreateSubmissionRequest(Guid? HomeworkAssignmentId, Guid? LiveExamSessionId);
