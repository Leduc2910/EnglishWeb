using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace EnglishTestWeb.Api.Infrastructure.Authorization.Handlers;

public sealed class ClassTeacherAuthorizationHandler(IClassAuthorizationService classAuthorizationService)
    : AuthorizationHandler<ClassTeacherViewRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClassTeacherViewRequirement requirement,
        Guid classId)
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

        var decision = await classAuthorizationService.RequireTeacherClassAccessAsync(classId, teacherId);
        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}
