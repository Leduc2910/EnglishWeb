namespace EnglishTestWeb.Api.Contracts.Classes;

public sealed record ClassCurrentResponse(
    Guid ClassId,
    string ClassName,
    string ClassCode,
    string Status);
