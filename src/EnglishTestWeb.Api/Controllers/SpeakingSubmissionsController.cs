using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.Speaking;
using EnglishTestWeb.Api.Contracts.Speaking;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/speaking-submissions")]
public sealed class SpeakingSubmissionsController(
    ISpeakingSubmissionService speakingSubmissionService,
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost]
    public async Task<ActionResult<SpeakingSubmissionDto>> CreateOrResume(
        [FromBody] CreateSpeakingSubmissionRequest request,
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
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status422UnprocessableEntity,
                "speaking.invalidSource",
                "No active class.",
                "Student has no active class context.");
        }

        var result = await speakingSubmissionService.CreateOrResumeAsync(
            studentId, activeClassId.Value, request, cancellationToken);

        if (!result.Success || result.Dto is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "speaking.invalidSource" => StatusCodes.Status422UnprocessableEntity,
                "speaking.sourceUnavailable" => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "speaking.invalidSource",
                "Cannot create speaking submission.",
                "The submission source is invalid or unavailable.");
        }

        return Ok(result.Dto);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpeakingSubmissionDto>> Get(
        Guid id,
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

        var result = await speakingSubmissionService.GetAsync(id, studentId, cancellationToken);

        if (!result.Success || result.Dto is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status404NotFound,
                result.ErrorCode ?? "speaking.notFound",
                "Speaking submission not found.",
                "The requested speaking submission could not be found.");
        }

        return Ok(result.Dto);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost("{id:guid}/upload-draft")]
    [RequestSizeLimit(110_000_000)]
    public async Task<ActionResult<SpeakingSubmissionDto>> UploadDraft(
        Guid id,
        IFormFile file,
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

        if (file is null || file.Length == 0)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status422UnprocessableEntity,
                "speaking.emptyFile",
                "No file provided.",
                "A file must be included in the request.");
        }

        var result = await speakingSubmissionService.UploadDraftAsync(id, studentId, file, cancellationToken);

        if (!result.Success || result.Dto is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "speaking.notFound" => StatusCodes.Status404NotFound,
                "speaking.alreadySubmitted" => StatusCodes.Status409Conflict,
                "speaking.invalidFileType" => StatusCodes.Status422UnprocessableEntity,
                "speaking.fileTooLarge" => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "speaking.invalidFileType",
                "Upload failed.",
                "The file could not be uploaded.");
        }

        return Ok(result.Dto);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost("{id:guid}/final-submit")]
    public async Task<ActionResult<SpeakingSubmissionDto>> FinalSubmit(
        Guid id,
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

        var result = await speakingSubmissionService.FinalSubmitAsync(id, studentId, cancellationToken);

        if (!result.Success || result.Dto is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "speaking.notFound" => StatusCodes.Status404NotFound,
                "speaking.fileRequired" => StatusCodes.Status422UnprocessableEntity,
                "speaking.sourceUnavailable" => StatusCodes.Status422UnprocessableEntity,
                "speaking.alreadySubmitted" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "speaking.invalidState",
                "Final submit failed.",
                "Cannot finalize this speaking submission.");
        }

        return Ok(result.Dto);
    }
}
