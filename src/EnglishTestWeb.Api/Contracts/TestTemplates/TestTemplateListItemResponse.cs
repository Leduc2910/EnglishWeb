namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record TestTemplateListItemResponse(
    Guid TemplateId,
    string Title,
    string Skill,
    string Status,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset UpdatedAt);
