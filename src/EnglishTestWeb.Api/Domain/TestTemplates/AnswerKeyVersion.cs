namespace EnglishTestWeb.Api.Domain.TestTemplates;

public sealed class AnswerKeyVersion
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public string Status { get; set; } = AnswerKeyStatuses.Draft;

    public string ScoringMode { get; set; } = ScoringModes.Equal;

    public int QuestionCount { get; set; }

    public decimal? TotalScore { get; set; }

    public string RowsJson { get; set; } = "[]";

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public TestTemplate? Template { get; set; }
}
