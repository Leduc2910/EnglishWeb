namespace EnglishTestWeb.Api.Contracts.Speaking;

public sealed record SpeakingSubmissionDto(
    Guid Id,
    string Status,
    string Mode,
    string TemplateTitle,
    string TemplateSkill,
    string ClassName,
    bool IsSourceOpen,
    string? CueMaterialFileId,
    string? CueMaterialFileName,
    DraftFileDto? DraftFile,
    DateTimeOffset? SubmittedAt);

public sealed record DraftFileDto(
    Guid FileId,
    string OriginalFileName,
    long SizeBytes,
    DateTimeOffset UploadedAt);
