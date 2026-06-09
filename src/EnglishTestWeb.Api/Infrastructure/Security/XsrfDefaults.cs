namespace EnglishTestWeb.Api.Infrastructure.Security;

public static class XsrfDefaults
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";
}
