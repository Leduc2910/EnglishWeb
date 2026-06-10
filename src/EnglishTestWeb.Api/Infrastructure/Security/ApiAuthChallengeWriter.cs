using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Infrastructure.Security;

internal static class ApiAuthChallengeWriter
{
    internal static Task WriteUnauthorizedAsync(HttpContext context) =>
        WriteAsync(context, StatusCodes.Status401Unauthorized, "auth.unauthorized");

    internal static Task WriteForbiddenAsync(HttpContext context) =>
        WriteAsync(context, StatusCodes.Status403Forbidden, "auth.forbidden");

    private static Task WriteAsync(HttpContext context, int statusCode, string code)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized." : "Forbidden.",
            Type = $"https://englishtestweb.local/problems/{code}",
            Detail = statusCode == StatusCodes.Status401Unauthorized
                ? "Authentication is required."
                : "The authenticated user does not have permission to access this resource."
        };
        problem.Extensions["code"] = code;

        return context.Response.WriteAsJsonAsync(problem);
    }
}
