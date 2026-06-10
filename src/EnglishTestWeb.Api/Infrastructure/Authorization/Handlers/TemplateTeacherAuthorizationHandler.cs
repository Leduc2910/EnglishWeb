using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace EnglishTestWeb.Api.Infrastructure.Authorization.Handlers;

public sealed class TemplateTeacherAuthorizationHandler(ITemplateAuthorizationService templateAuthorizationService)
    : AuthorizationHandler<TemplateTeacherViewRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TemplateTeacherViewRequirement requirement,
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

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(templateId, teacherId);
        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}
