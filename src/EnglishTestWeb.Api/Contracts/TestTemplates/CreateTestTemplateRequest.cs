namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record CreateTestTemplateRequest(
    string Title,
    string Skill,
    string? Description,
    IReadOnlyList<string>? Tags);
