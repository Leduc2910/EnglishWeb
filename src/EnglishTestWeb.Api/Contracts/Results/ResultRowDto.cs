namespace EnglishTestWeb.Api.Contracts.Results;

public sealed record ResultRowDto(
    Guid Id,
    string Type,           // "reading-listening" | "speaking"
    string Mode,           // "homework" | "live-exam"
    string StudentName,
    string StudentId,
    Guid ClassId,
    string ClassName,
    Guid TemplateId,
    string TemplateTitle,
    string Skill,          // "reading" | "listening" | "speaking"
    string Status,
    decimal? Score,        // AutoScore (R/L) hoặc Speaking Score cast sang decimal
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt);
