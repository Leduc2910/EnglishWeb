using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.Submissions;
using EnglishTestWeb.Api.Contracts.Submissions;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/submissions")]
public sealed class SubmissionsController(
    ISubmissionService submissionService,
    IProtectedFileService protectedFileService,
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost]
    public async Task<ActionResult<SubmissionDto>> CreateOrResume(
        [FromBody] CreateSubmissionRequest request,
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
                "submission.invalidSource",
                "No active class.",
                "Student has no active class context.");
        }

        var result = await submissionService.CreateOrResumeAsync(studentId, activeClassId.Value, request, cancellationToken);

        if (!result.Success || result.SubmissionId is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "submission.invalidSource" => StatusCodes.Status422UnprocessableEntity,
                "submission.sourceUnavailable" => StatusCodes.Status422UnprocessableEntity,
                "submission.notFound" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return hiddenResourceResponseFactory.FromCode(
                statusCode,
                result.ErrorCode ?? "submission.invalidSource",
                "Cannot create submission.",
                "The submission source is invalid or unavailable.");
        }

        var workspace = await submissionService.GetWorkspaceAsync(result.SubmissionId.Value, studentId, cancellationToken);
        if (workspace is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status404NotFound,
                "submission.notFound",
                "Submission not found.",
                "The created submission could not be retrieved.");
        }

        var dto = new SubmissionDto(workspace.Id, workspace.Status, workspace.Mode);

        if (result.Created)
            return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id }, dto);

        return Ok(dto);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpGet("{id:guid}/workspace")]
    public async Task<ActionResult<SubmissionWorkspaceDto>> GetWorkspace(
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

        var workspace = await submissionService.GetWorkspaceAsync(id, studentId, cancellationToken);
        if (workspace is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status404NotFound,
                "submission.notFound",
                "Submission not found.",
                "The requested submission could not be found.");
        }

        return Ok(workspace);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPut("{id:guid}/answers")]
    public async Task<ActionResult> AutosaveAnswers(
        Guid id,
        [FromBody] AutosaveAnswersRequest request,
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

        var result = await submissionService.AutosaveAnswersAsync(id, studentId, request, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "submission.notDraft" => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status409Conflict,
                    "submission.notDraft",
                    "Cannot autosave.",
                    "The submission has already been submitted."),
                _ => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status404NotFound,
                    result.ErrorCode ?? "submission.notFound",
                    "Submission not found.",
                    "The requested submission could not be found."),
            };
        }

        return NoContent();
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<SubmissionResultDto>> FinalSubmit(
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

        var result = await submissionService.FinalSubmitAsync(id, studentId, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "submission.sourceUnavailable" => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status422UnprocessableEntity,
                    "submission.sourceUnavailable",
                    "Cannot submit.",
                    "The submission source is no longer available (deadline passed or session closed)."),
                _ => hiddenResourceResponseFactory.FromCode(
                    StatusCodes.Status404NotFound,
                    result.ErrorCode ?? "submission.notFound",
                    "Submission not found.",
                    "The requested submission could not be found."),
            };
        }

        return Ok(result.Result);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpGet("{id:guid}/materials/{fileId:guid}/content")]
    public async Task<ActionResult> GetMaterialContent(
        Guid id,
        Guid fileId,
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

        var result = await protectedFileService.OpenForStudentWithSubmissionAsync(fileId, studentId, id, cancellationToken);
        if (!result.Allowed || result.File is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status404NotFound,
                result.ErrorCode ?? "files.notFound",
                "File not found.",
                "The requested file could not be found.");
        }

        Response.Headers[HeaderNames.AcceptRanges] = "bytes";
        return File(
            result.File.Content,
            result.File.ContentType,
            result.File.OriginalFileName,
            enableRangeProcessing: true);
    }
}
