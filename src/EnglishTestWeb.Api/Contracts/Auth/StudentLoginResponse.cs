namespace EnglishTestWeb.Api.Contracts.Auth;

public sealed record StudentLoginResponse(
    string UserId,
    string? Email,
    string? UserName,
    IReadOnlyList<string> Roles,
    ActiveClassResponse ActiveClass);
