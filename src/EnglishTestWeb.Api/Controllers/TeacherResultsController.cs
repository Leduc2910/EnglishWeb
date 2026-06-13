using EnglishTestWeb.Api.Application.Results;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Results;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/teacher/results")]
[Authorize(Roles = IdentityRoleNames.Teacher)]
public sealed class TeacherResultsController(
    IResultsService resultsService,
    ITeacherSubmissionDetailService submissionDetailService,
    ICurrentUserContext currentUserContext,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResultsPageDto>> GetResults(
        [FromQuery] Guid? classId,
        [FromQuery] string? mode,
        [FromQuery] Guid? templateId,
        [FromQuery] string? q,
        [FromQuery] string? skill,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "submittedAt",
        [FromQuery] string direction = "desc",
        CancellationToken cancellationToken = default)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var filter = new ResultsFilter(
            ClassId:    classId,
            Mode:       mode,
            TemplateId: templateId,
            Q:          q,
            Skill:      skill,
            Status:     status,
            Page:       page,
            PageSize:   pageSize,
            Sort:       sort,
            Direction:  direction);

        var result = await resultsService.GetResultsForTeacherAsync(teacherId, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<TeacherSubmissionDetailDto>> GetSubmissionDetail(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status401Unauthorized,
                "auth.unauthorized", "Unauthorized.", "Authentication required.");

        var (success, errorCode, dto) = await submissionDetailService.GetForTeacherAsync(
            submissionId, teacherId, cancellationToken);

        if (!success)
            return hiddenResourceResponseFactory.FromCode(StatusCodes.Status404NotFound,
                errorCode!, "Not found.", "Submission not found or out of scope.");

        return Ok(dto);
    }
}
