using EnglishTestWeb.Api.Application.HomeworkAssignments;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.HomeworkAssignments;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/homework-assignments")]
public sealed class HomeworkAssignmentsController(
    IHomeworkAssignmentService homeworkAssignmentService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateHomeworkAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized.",
                "Authentication is required.");
        }

        var result = await homeworkAssignmentService.CreateAsync(teacherId, request, cancellationToken);

        if (!result.Allowed || result.Detail is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "homework.createFailed",
                "Homework creation failed.",
                result.ErrorCode ?? "homework.createFailed");
        }

        return StatusCode(StatusCodes.Status201Created, result.Detail);
    }
}
