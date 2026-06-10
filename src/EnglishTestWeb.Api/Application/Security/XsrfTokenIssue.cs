namespace EnglishTestWeb.Api.Application.Security;

public sealed record XsrfTokenIssue(string CookieName, string HeaderName, string RequestToken);
