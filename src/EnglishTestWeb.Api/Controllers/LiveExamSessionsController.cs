using EnglishTestWeb.Api.Application.LiveExamSessions;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.LiveExamSessions;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/live-exam-sessions")]
public sealed class LiveExamSessionsController(
    ILiveExamSessionService liveExamSessionService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateLiveExamSessionRequest request,
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

        var result = await liveExamSessionService.CreateAsync(teacherId, request, cancellationToken);

        if (!result.Allowed || result.Detail is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "liveExam.createFailed",
                "Live exam session creation failed.",
                result.ErrorCode ?? "liveExam.createFailed");
        }

        return StatusCode(StatusCodes.Status201Created, result.Detail);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost("{id:guid}/open")]
    public async Task<ActionResult> Open(Guid id, CancellationToken cancellationToken)
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

        var result = await liveExamSessionService.OpenAsync(teacherId, id, cancellationToken);

        if (!result.Allowed || result.Detail is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "liveExam.transitionFailed",
                "Live exam session open failed.",
                result.ErrorCode ?? "liveExam.transitionFailed");
        }

        return Ok(result.Detail);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult> Close(Guid id, CancellationToken cancellationToken)
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

        var result = await liveExamSessionService.CloseAsync(teacherId, id, cancellationToken);

        if (!result.Allowed || result.Detail is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "liveExam.transitionFailed",
                "Live exam session close failed.",
                result.ErrorCode ?? "liveExam.transitionFailed");
        }

        return Ok(result.Detail);
    }
}
