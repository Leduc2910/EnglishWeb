namespace EnglishTestWeb.Api.Domain.Classes;

public sealed class ClassMembership
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public SchoolClass Class { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = ClassStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; }
}
