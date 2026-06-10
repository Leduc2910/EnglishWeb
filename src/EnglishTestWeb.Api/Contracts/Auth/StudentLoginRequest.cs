namespace EnglishTestWeb.Api.Contracts.Auth;

public sealed record StudentLoginRequest(
    string Identifier,
    string Password,
    string ClassCode,
    bool RememberMe = false);
