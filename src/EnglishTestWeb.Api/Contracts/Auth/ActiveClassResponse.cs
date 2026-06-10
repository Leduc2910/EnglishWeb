namespace EnglishTestWeb.Api.Contracts.Auth;

public sealed record ActiveClassResponse(
    Guid ClassId,
    string ClassName,
    string ClassCode);
