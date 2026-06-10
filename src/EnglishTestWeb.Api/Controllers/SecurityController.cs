using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/security")]
public sealed class SecurityController(IXsrfTokenService xsrfTokenService) : ControllerBase
{
    [HttpGet("xsrf-token")]
    public ActionResult<XsrfTokenResponse> IssueXsrfToken()
    {
        var token = xsrfTokenService.Issue(HttpContext);
        return Ok(new XsrfTokenResponse(token.CookieName, token.HeaderName, token.RequestToken));
    }
}
