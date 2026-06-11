namespace EnglishTestWeb.Api.Domain.TestTemplates;

public sealed class TestTemplate
{
    public Guid Id { get; set; }

    public string TeacherId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Skill { get; set; } = TemplateSkill.Reading;

    public string? Description { get; set; }

    public string? TagsJson { get; set; }

    public string Status { get; set; } = TemplateStatuses.Draft;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }
}
