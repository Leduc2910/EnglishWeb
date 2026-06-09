namespace EnglishTestWeb.Api.Infrastructure.Storage;

public sealed class ProtectedStorageOptions
{
    public const string SectionName = "ProtectedStorage";

    public string? RootPath { get; init; }
}
