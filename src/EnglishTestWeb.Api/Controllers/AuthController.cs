using EnglishTestWeb.Api.Application.Auth;
using EnglishTestWeb.Api.Contracts.Auth;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IWebHostEnvironment environment) : ControllerBase
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
            return AuthProblem(
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

            return AuthProblem(
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
            return AuthProblem(
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
            return AuthProblem(
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
        if (!User.IsInRole(IdentityRoleNames.Teacher))
        {
            return AuthProblem(
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "Forbidden.",
                "The authenticated user does not have permission to access this resource.");
        }

        return Ok(new TeacherPingResponse("ok"));
    }

    private ActionResult AuthProblem(int statusCode, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://englishtestweb.local/problems/{code}",
            Detail = detail
        };
        problem.Extensions["code"] = code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
