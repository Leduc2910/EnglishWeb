namespace EnglishTestWeb.Api.Domain.Classes;

public sealed class SchoolClass
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ClassCode { get; set; } = string.Empty;

    public string TeacherId { get; set; } = string.Empty;

    public string Status { get; set; } = ClassStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ClassMembership> Memberships { get; set; } = [];
}
