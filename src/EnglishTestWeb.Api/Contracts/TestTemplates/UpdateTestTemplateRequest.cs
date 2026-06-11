namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record UpdateTestTemplateRequest(
    string Title,
    string Skill,
    string? Description,
    IReadOnlyList<string>? Tags);
