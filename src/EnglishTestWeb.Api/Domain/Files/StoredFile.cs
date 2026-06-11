namespace EnglishTestWeb.Api.Domain.Files;

public sealed class StoredFile
{
    public Guid Id { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string? ChecksumSha256 { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    public string Status { get; set; } = StoredFileStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
