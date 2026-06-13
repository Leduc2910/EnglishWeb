using EnglishTestWeb.Api.Application.Dashboard;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Dashboard;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/teacher/dashboard")]
[Authorize(Roles = IdentityRoleNames.Teacher)]
public sealed class TeacherDashboardController(
    ITeacherDashboardService dashboardService,
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TeacherDashboardDto>> GetDashboard(
        [FromQuery] Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var dto = await dashboardService.GetDashboardAsync(teacherId, classId, cancellationToken);
        return Ok(dto);
    }
}
