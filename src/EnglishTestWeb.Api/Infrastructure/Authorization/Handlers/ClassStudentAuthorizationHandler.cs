using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace EnglishTestWeb.Api.Infrastructure.Authorization.Handlers;

public sealed class ClassStudentAuthorizationHandler(IClassAuthorizationService classAuthorizationService)
    : AuthorizationHandler<ClassStudentViewRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClassStudentViewRequirement requirement,
        Guid classId)
    {
        if (!context.User.IsInRole(IdentityRoleNames.Student))
        {
            return;
        }

        var studentId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return;
        }

        var decision = await classAuthorizationService.RequireStudentClassAccessAsync(classId, studentId);
        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}
