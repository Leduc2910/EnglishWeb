namespace EnglishTestWeb.Api.Domain.TestTemplates;

public sealed class TestMaterial
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public Guid StoredFileId { get; set; }

    public string Role { get; set; } = MaterialRoles.Pdf;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public TestTemplate? Template { get; set; }

    public Files.StoredFile? StoredFile { get; set; }
}
