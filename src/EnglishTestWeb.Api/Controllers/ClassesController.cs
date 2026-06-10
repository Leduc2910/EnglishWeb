using EnglishTestWeb.Api.Application.Classes;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/classes")]
public sealed class ClassesController(IClassService classService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult> LookupByCode(string code, CancellationToken cancellationToken)
    {
        var result = await classService.LookupByCodeAsync(code, cancellationToken);
        if (!result.Found || result.Class is null)
        {
            return ClassProblem(
                StatusCodes.Status404NotFound,
                result.ErrorCode ?? "classes.codeNotFound",
                "Class lookup failed.",
                "The requested class code could not be resolved.");
        }

        return Ok(result.Class);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet]
    public async Task<ActionResult> GetTeacherClasses(CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return ClassProblem(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        var classes = await classService.GetTeacherClassesAsync(teacherId, cancellationToken);
        return Ok(classes);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetClassDetail(Guid id, CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return ClassProblem(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        var result = await classService.GetClassDetailForTeacherAsync(id, teacherId, cancellationToken);
        if (!result.Allowed || result.Detail is null)
        {
            return ClassProblem(
                StatusCodes.Status403Forbidden,
                result.ErrorCode ?? "classes.forbidden",
                "Forbidden.",
                "The authenticated user does not have permission to access this class.");
        }

        return Ok(result.Detail);
    }

    private ActionResult ClassProblem(int statusCode, string code, string title, string detail)
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
