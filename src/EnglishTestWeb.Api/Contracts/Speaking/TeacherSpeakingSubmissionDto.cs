namespace EnglishTestWeb.Api.Contracts.Speaking;

public sealed record TeacherSpeakingSubmissionDto(
    Guid Id,
    string StudentName,
    string ClassName,
    string TemplateTitle,
    string Mode,
    string Status,
    DateTimeOffset? SubmittedAt,
    string? SubmittedFileName,
    long? SubmittedFileSizeBytes,
    string? SubmittedFileId,
    bool IsFileMissing,
    int? Score,
    string? Feedback,
    string? GraderId,
    DateTimeOffset? GradedAt);
