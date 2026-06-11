using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(
    IProtectedFileService protectedFileService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet("{fileId:guid}/content")]
    public async Task<ActionResult> GetContent(Guid fileId, CancellationToken cancellationToken)
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

        var result = await protectedFileService.OpenForAuthorizedUserAsync(fileId, teacherId, cancellationToken);
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
