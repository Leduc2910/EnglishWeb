using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Application.Security;

public interface IXsrfTokenService
{
    XsrfTokenIssue Issue(HttpContext httpContext);
}
