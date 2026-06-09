using EnglishTestWeb.Api.Application.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace EnglishTestWeb.Api.Infrastructure.Security;

public sealed class XsrfTokenService(IAntiforgery antiforgery, IWebHostEnvironment environment) : IXsrfTokenService
{
    public XsrfTokenIssue Issue(HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        if (tokens.RequestToken is null)
        {
            throw new InvalidOperationException("Unable to create an XSRF request token.");
        }

        httpContext.Response.Cookies.Append(
            XsrfDefaults.CookieName,
            tokens.RequestToken,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

        return new XsrfTokenIssue(XsrfDefaults.CookieName, XsrfDefaults.HeaderName);
    }
}
