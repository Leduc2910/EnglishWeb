using EnglishTestWeb.Api.Application.AssignedTests;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.AssignedTests;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/assigned-tests")]
public sealed class AssignedTestsController(
    IAssignedTestService assignedTestService,
    IClassAuthorizationService classAuthorizationService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpGet]
    public async Task<ActionResult<AssignedTestsResponse>> GetForActiveClass(
        CancellationToken cancellationToken)
    {
        var studentId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        var activeClassId = currentUserContext.ActiveClassId;
        if (activeClassId is null)
        {
            return Ok(new AssignedTestsResponse([]));
        }

        var authDecision = await classAuthorizationService.RequireStudentClassAccessAsync(
            activeClassId.Value, studentId, cancellationToken);

        if (!authDecision.IsAllowed)
        {
            return hiddenResourceResponseFactory.FromDecision(authDecision);
        }

        var items = await assignedTestService.GetForStudentAsync(
            studentId, activeClassId.Value, cancellationToken);

        return Ok(new AssignedTestsResponse(items));
    }
}
