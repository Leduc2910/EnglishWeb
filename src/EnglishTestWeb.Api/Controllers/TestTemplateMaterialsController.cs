using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Authorization.Policies;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/test-templates/{templateId:guid}/materials")]
public sealed class TestTemplateMaterialsController(
    ITestTemplateMaterialService materialService,
    ITemplateAuthorizationService templateAuthorizationService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext,
    AuthorizationDenialAuditor denialAuditor,
    IAuthorizationService authorizationService) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet]
    public async Task<ActionResult> List(Guid templateId, CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacherId();
        if (teacherId is null)
        {
            return UnauthorizedResponse();
        }

        if (!await AuthorizeViewAsync(templateId, teacherId, cancellationToken))
        {
            return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
        }

        var result = await materialService.ListMaterialsAsync(templateId, teacherId, cancellationToken);
        if (!result.Allowed || result.Response is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "templates.notFound",
                "Template materials unavailable.",
                "The requested template materials could not be loaded.");
        }

        return Ok(result.Response);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult> Upload(
        Guid templateId,
        [FromForm] string role,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacherId();
        if (teacherId is null)
        {
            return UnauthorizedResponse();
        }

        if (!await AuthorizeEditAsync(templateId, teacherId, cancellationToken))
        {
            return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status400BadRequest,
                "materials.roleInvalid",
                "Invalid material role.",
                "A valid material role is required.");
        }

        if (file is null || file.Length == 0)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status400BadRequest,
                "files.invalidType",
                "Invalid file.",
                "A valid file is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await materialService.UploadMaterialAsync(
            templateId,
            teacherId,
            role,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        if (!result.Succeeded || result.Response is null)
        {
            if (string.Equals(result.ErrorCode, "templates.notFound", StringComparison.Ordinal))
            {
                return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
            }

            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "materials.uploadFailed",
                "Material upload failed.",
                "The material upload could not be processed.");
        }

        return StatusCode(result.StatusCode, result.Response);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpDelete("{materialId:guid}")]
    public async Task<ActionResult> Remove(
        Guid templateId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacherId();
        if (teacherId is null)
        {
            return UnauthorizedResponse();
        }

        if (!await AuthorizeEditAsync(templateId, teacherId, cancellationToken))
        {
            return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
        }

        var result = await materialService.RemoveMaterialAsync(templateId, teacherId, materialId, cancellationToken);
        if (!result.Succeeded)
        {
            if (string.Equals(result.ErrorCode, "templates.notFound", StringComparison.Ordinal))
            {
                return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
            }

            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "materials.notFound",
                "Material removal failed.",
                "The material could not be removed.");
        }

        return NoContent();
    }

    private string? RequireTeacherId() => currentUserContext.UserId;

    private ActionResult UnauthorizedResponse() =>
        hiddenResourceResponseFactory.FromCode(
            StatusCodes.Status401Unauthorized,
            "auth.unauthorized",
            "Unauthorized.",
            "Authentication is required.");

    private async Task<bool> AuthorizeViewAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            templateId,
            AuthorizationPolicies.CanViewTemplateAsTeacher);

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        return authorizationResult.Succeeded && decision.IsAllowed;
    }

    private async Task<bool> AuthorizeEditAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            templateId,
            AuthorizationPolicies.CanEditTemplateAsTeacher);

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        return authorizationResult.Succeeded && decision.IsAllowed;
    }

    private async Task<ActionResult> HiddenTemplateResponseAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        denialAuditor.AuditDenied(decision, "test-template", templateId.ToString());
        return hiddenResourceResponseFactory.FromDecision(decision);
    }
}
