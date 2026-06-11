namespace EnglishTestWeb.Api.Contracts.TestTemplates;

public sealed record TestMaterialResponse(
    Guid MaterialId,
    Guid FileId,
    string Role,
    string OriginalFileName,
    long SizeBytes,
    string ContentType,
    DateTimeOffset UploadedAt);

public sealed record TestMaterialListResponse(IReadOnlyList<TestMaterialResponse> Items);
