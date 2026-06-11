namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record TestTemplateDetailResponse(
    Guid TemplateId,
    string Title,
    string Skill,
    string? Description,
    IReadOnlyList<string> Tags,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? ArchivedAt);
