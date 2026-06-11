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
[Route("api/test-templates/{templateId:guid}/answer-key")]
public sealed class AnswerKeyController(
    IAnswerKeyService answerKeyService,
    ITemplateAuthorizationService templateAuthorizationService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext,
    AuthorizationDenialAuditor denialAuditor,
    IAuthorizationService authorizationService) : ControllerBase
{
    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet]
    public async Task<ActionResult> Get(Guid templateId, CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        if (!await AuthorizeAsync(templateId, AuthorizationPolicies.CanViewTemplateAsTeacher))
        {
            return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
        }

        var result = await answerKeyService.GetAsync(templateId, teacherId, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "answerKey.notFound",
                "Answer key unavailable.",
                "The requested answer key could not be loaded.");
        }

        return Ok(result.Response);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpPut]
    public async Task<ActionResult> Upsert(
        Guid templateId,
        [FromBody] UpsertAnswerKeyRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = currentUserContext.UserId;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        if (!await AuthorizeAsync(templateId, AuthorizationPolicies.CanEditTemplateAsTeacher))
        {
            return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
        }

        var result = await answerKeyService.UpsertDraftAsync(templateId, teacherId, request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            if (string.Equals(result.ErrorCode, "templates.notFound", StringComparison.Ordinal))
            {
                return await HiddenTemplateResponseAsync(templateId, teacherId, cancellationToken);
            }

            return hiddenResourceResponseFactory.FromCode(
                result.StatusCode,
                result.ErrorCode ?? "answerKey.error",
                "Answer key update failed.",
                "The answer key request could not be processed.");
        }

        return Ok(result.Response);
    }

    private ActionResult UnauthorizedResponse() =>
        hiddenResourceResponseFactory.FromCode(
            StatusCodes.Status401Unauthorized,
            "auth.unauthorized",
            "Unauthorized.",
            "Authentication is required.");

    private async Task<bool> AuthorizeAsync(Guid templateId, string policyName)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            templateId,
            policyName);

        return authorizationResult.Succeeded;
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
