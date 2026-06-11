using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Infrastructure.Authorization.Handlers;

public sealed class TemplateTeacherEditAuthorizationHandler(ITemplateAuthorizationService templateAuthorizationService)
    : AuthorizationHandler<TemplateTeacherEditRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TemplateTeacherEditRequirement requirement,
        Guid templateId)
    {
        if (!context.User.IsInRole(IdentityRoleNames.Teacher))
        {
            return;
        }

        var teacherId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);
        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}
