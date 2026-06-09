using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Infrastructure.Security;

public static class XsrfProtectionMiddleware
{
    public static IApplicationBuilder UseApiXsrfProtection(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (!RequiresValidation(context.Request))
            {
                await next(context);
                return;
            }

            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var hasHeader = context.Request.Headers.ContainsKey(XsrfDefaults.HeaderName);

            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                await WriteXsrfProblemAsync(context, hasHeader ? "auth.xsrfInvalid" : "auth.xsrfRequired");
                return;
            }

            await next(context);
        });
    }

    private static bool RequiresValidation(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && (HttpMethods.IsPost(request.Method)
                || HttpMethods.IsPut(request.Method)
                || HttpMethods.IsPatch(request.Method)
                || HttpMethods.IsDelete(request.Method));
    }

    private static async Task WriteXsrfProblemAsync(HttpContext context, string code)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = code == "auth.xsrfRequired" ? "XSRF token is required." : "XSRF token is invalid.",
            Type = $"https://englishtestweb.local/problems/{code}",
            Detail = "Unsafe API requests must include a valid XSRF header."
        };
        problem.Extensions["code"] = code;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonSerializerOptions.Web));
    }
}
