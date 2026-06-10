using EnglishTestWeb.Api.Application.Auth;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Auth;
using EnglishTestWeb.Api.Infrastructure.Audit;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext,
    AuthorizationDenialAuditor denialAuditor,
    IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginTeacherAsync(request, cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.loginInvalid",
                "Login failed.",
                "The supplied credentials are invalid.");
        }

        return Ok(result.User);
    }

    [AllowAnonymous]
    [HttpPost("student/login")]
    public async Task<ActionResult<StudentLoginResponse>> StudentLogin(
        [FromBody] StudentLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginStudentAsync(request, cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "classes.codeNotFound" or "classes.codeInactive" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status401Unauthorized
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "auth.loginInvalid",
                "Login failed.",
                "The supplied credentials or class context is invalid.");
        }

        return Ok(result.User);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(User, cancellationToken);
        if (user is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        return Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("testing/sign-in")]
    public async Task<IActionResult> TestingSignIn(
        [FromBody] TestingSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        var result = await authService.SignInForTestingAsync(request, cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.loginInvalid",
                "Login failed.",
                "The supplied credentials are invalid.");
        }

        return Ok(result.User);
    }

    [Authorize]
    [HttpGet("teacher/ping")]
    public IActionResult TeacherPing()
    {
        if (!currentUserContext.IsInRole(IdentityRoleNames.Teacher))
        {
            var decision = AuthorizationDecision.Forbidden(
                "auth.forbidden",
                AuthorizationDenialReason.WrongRole);
            denialAuditor.AuditDenied(decision, "auth", "teacher/ping");
            return hiddenResourceResponseFactory.FromDecision(decision);
        }

        return Ok(new TeacherPingResponse("ok"));
    }
}
