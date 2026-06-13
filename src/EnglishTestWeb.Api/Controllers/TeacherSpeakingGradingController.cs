using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.Speaking;
using EnglishTestWeb.Api.Contracts.Speaking;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/teacher/speaking-submissions")]
[Authorize(Roles = IdentityRoleNames.Teacher)]
public sealed class TeacherSpeakingGradingController(
    ITeacherSpeakingGradingService gradingService,
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    IFileStorage fileStorage,
    EnglishTestWebDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherSpeakingSubmissionDto>> Get(
        Guid id, CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var result = await gradingService.GetForTeacherAsync(id, teacherId, cancellationToken);
        if (!result.Success || result.Dto is null)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                result.ErrorCode ?? "speaking.notFound", "Not found.", "Speaking submission not found.");

        return Ok(result.Dto);
    }

    [HttpPost("{id:guid}/grade")]
    public async Task<ActionResult<TeacherSpeakingSubmissionDto>> Grade(
        Guid id, [FromBody] GradeSpeakingRequest request, CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var result = await gradingService.GradeAsync(id, teacherId, request, cancellationToken);
        if (!result.Success || result.Dto is null)
        {
            var statusCode = result.ErrorCode switch
            {
                "speaking.notFound" => StatusCodes.Status404NotFound,
                "speaking.scoreInvalid" => StatusCodes.Status422UnprocessableEntity,
                "speaking.notSubmitted" => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status422UnprocessableEntity,
            };
            return hiddenResourceResponseFactory.FromCode(statusCode,
                result.ErrorCode ?? "speaking.gradeFailed", "Grade failed.", "Cannot grade this submission.");
        }

        return Ok(result.Dto);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<ActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var submission = await db.SpeakingSubmissions
            .Include(s => s.HomeworkAssignment)
            .Include(s => s.LiveExamSession)
            .Include(s => s.DraftStoredFile)
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                "speaking.notFound", "Not found.", "Speaking submission not found.");

        var sourceTeacherId = submission.HomeworkAssignment?.TeacherId
                           ?? submission.LiveExamSession?.TeacherId;
        if (sourceTeacherId != teacherId)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                "speaking.notFound", "Not found.", "Speaking submission not found.");

        var file = submission.DraftStoredFile;
        if (file is null)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                "files.notFound", "File not found.", "No submitted file.");

        try
        {
            var stream = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
            Response.Headers[HeaderNames.AcceptRanges] = "bytes";
            return File(stream, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                "files.notFound", "File not found.", "The submitted audio file is missing from storage.");
        }
    }
}
