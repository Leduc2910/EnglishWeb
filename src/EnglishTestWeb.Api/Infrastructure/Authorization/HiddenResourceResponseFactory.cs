using EnglishTestWeb.Api.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Infrastructure.Authorization;

public sealed class HiddenResourceResponseFactory : IHiddenResourceResponseFactory
{
    public ActionResult FromDecision(AuthorizationDecision decision)
    {
        return decision.Outcome switch
        {
            AuthorizationOutcome.Allowed => throw new InvalidOperationException("Cannot build a problem response for an allowed decision."),
            AuthorizationOutcome.HiddenNotFound => FromCode(
                StatusCodes.Status404NotFound,
                decision.ErrorCode ?? "classes.notFound",
                "Not found.",
                "The requested class could not be found."),
            AuthorizationOutcome.Forbidden => FromCode(
                StatusCodes.Status403Forbidden,
                decision.ErrorCode ?? "auth.forbidden",
                "Forbidden.",
                "The authenticated user does not have permission to access this resource."),
            _ => FromCode(
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "Forbidden.",
                "The authenticated user does not have permission to access this resource.")
        };
    }

    public ActionResult FromCode(int statusCode, string code, string title, string detail)
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
