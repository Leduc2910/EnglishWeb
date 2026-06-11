using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Authorization.Policies;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/test-templates")]
public sealed class TestTemplatesController(
    ITestTemplateService testTemplateService,
    ITemplateAuthorizationService templateAuthorizationService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext,
    AuthorizationDenialAuditor denialAuditor,
    IAuthorizationService authorizationService) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet]
    public async Task<ActionResult> ListForTeacher(
        [FromQuery] TestTemplateListQuery query,
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

        var templates = await testTemplateService.ListForTeacherAsync(teacherId, query, cancellationToken);
        return Ok(templates);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
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

        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.CanViewTemplateAsTeacher);

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            id,
            teacherId,
            cancellationToken);

        if (!authorizationResult.Succeeded || !decision.IsAllowed)
        {
            denialAuditor.AuditDenied(decision, "test-template", id.ToString());
            return hiddenResourceResponseFactory.FromDecision(decision);
        }

        var result = await testTemplateService.GetByIdForTeacherAsync(id, teacherId, cancellationToken);
        if (!result.Allowed || result.Detail is null)
        {
            var fallbackDecision = AuthorizationDecision.HiddenNotFound(
                result.ErrorCode ?? "templates.notFound",
                AuthorizationDenialReason.TemplateOwnership);
            denialAuditor.AuditDenied(fallbackDecision, "test-template", id.ToString());
            return hiddenResourceResponseFactory.FromDecision(fallbackDecision);
        }

        return Ok(result.Detail);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateTestTemplateRequest request,
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

        var result = await testTemplateService.CreateDraftForTeacherAsync(teacherId, request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "templates.invalid",
                "Invalid template setup.",
                "The template setup request could not be processed.");
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Response.TemplateId }, result.Response);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateTestTemplateRequest request,
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

        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.CanEditTemplateAsTeacher);

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            id,
            teacherId,
            cancellationToken);

        if (!authorizationResult.Succeeded || !decision.IsAllowed)
        {
            denialAuditor.AuditDenied(decision, "test-template", id.ToString());
            return hiddenResourceResponseFactory.FromDecision(decision);
        }

        var result = await testTemplateService.UpdateDraftSetupForTeacherAsync(id, teacherId, request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            if (string.Equals(result.ErrorCode, "templates.notFound", StringComparison.Ordinal))
            {
                var notFoundDecision = AuthorizationDecision.HiddenNotFound(
                    result.ErrorCode ?? "templates.notFound",
                    AuthorizationDenialReason.TemplateOwnership);
                denialAuditor.AuditDenied(notFoundDecision, "test-template", id.ToString());
                return hiddenResourceResponseFactory.FromDecision(notFoundDecision);
            }

            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "templates.invalid",
                "Invalid template setup.",
                "The template setup request could not be processed.");
        }

        return Ok(result.Response);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPost("{id:guid}/mark-ready")]
    public async Task<ActionResult> MarkReady(Guid id, CancellationToken cancellationToken)
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

        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.CanViewTemplateAsTeacher);

        if (!authorizationResult.Succeeded)
        {
            return await HiddenTemplateResponseAsync(id, teacherId, cancellationToken);
        }

        var result = await testTemplateService.MarkReadyAsync(id, teacherId, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            if (string.Equals(result.ErrorCode, "templates.notFound", StringComparison.Ordinal))
            {
                return await HiddenTemplateResponseAsync(id, teacherId, cancellationToken);
            }

            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "review.markReadyFailed",
                "Mark ready failed.",
                "The template could not be marked ready.");
        }

        return Ok(result.Response);
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
