namespace EnglishTestWeb.Api.Contracts.Security;

public sealed record XsrfTokenResponse(string CookieName, string HeaderName, string RequestToken);
