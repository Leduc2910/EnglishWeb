namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed class TestTemplateListQuery
{
    public string? Skill { get; set; }

    public string? Status { get; set; }

    public string? Q { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
