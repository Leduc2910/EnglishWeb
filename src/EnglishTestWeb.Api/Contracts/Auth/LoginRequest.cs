namespace EnglishTestWeb.Api.Contracts.Auth;

public sealed record LoginRequest(string Identifier, string Password, bool RememberMe);
