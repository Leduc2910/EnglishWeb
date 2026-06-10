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
}
