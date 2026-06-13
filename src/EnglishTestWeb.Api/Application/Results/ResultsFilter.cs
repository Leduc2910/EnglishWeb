namespace EnglishTestWeb.Api.Application.Results;

public sealed record ResultsFilter(
    Guid? ClassId,
    string? Mode,          // "homework" | "live-exam" | null (all)
    Guid? TemplateId,
    string? Q,             // tìm kiếm tên/email học sinh
    string? Skill,         // "reading" | "listening" | "speaking" | null (all)
    string? Status,        // "draft" | "submitted" | "auto-graded" | "graded" | null (all)
    int Page,
    int PageSize,
    string Sort,           // default: "submittedAt"
    string Direction);     // "asc" | "desc"
