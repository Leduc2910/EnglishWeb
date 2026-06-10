using EnglishTestWeb.Api.Application.Classes;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Auth;
using EnglishTestWeb.Api.Contracts.Classes;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Authorization.Policies;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[Route("api/classes")]
public sealed class ClassesController(
    IClassService classService,
    IClassAuthorizationService classAuthorizationService,
    IHiddenResourceResponseFactory hiddenResourceResponseFactory,
    ICurrentUserContext currentUserContext,
    AuthorizationDenialAuditor denialAuditor,
    IAuthorizationService authorizationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult> LookupByCode(string code, CancellationToken cancellationToken)
    {
        var result = await classService.LookupByCodeAsync(code, cancellationToken);
        if (!result.Found || result.Class is null)
        {
            return hiddenResourceResponseFactory.FromCode(
                StatusCodes.Status404NotFound,
                result.ErrorCode ?? "classes.codeNotFound",
                "Class lookup failed.",
                "The requested class code could not be resolved.");
        }

        return Ok(result.Class);
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet]
    public async Task<ActionResult> GetTeacherClasses(CancellationToken cancellationToken)
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

        var classes = await classService.GetTeacherClassesAsync(teacherId, cancellationToken);
        return Ok(classes);
    }

    [Authorize(Roles = IdentityRoleNames.Student)]
    [HttpGet("current")]
    public async Task<ActionResult<ClassCurrentResponse>> GetCurrentClass(CancellationToken cancellationToken)
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
            var missingClaimDecision = AuthorizationDecision.HiddenNotFound(
                "classes.notFound",
                AuthorizationDenialReason.ClassMembership);
            denialAuditor.AuditDenied(missingClaimDecision, "class", null);
            return hiddenResourceResponseFactory.FromDecision(missingClaimDecision);
        }

        var decision = await classAuthorizationService.RequireStudentClassAccessAsync(
            activeClassId.Value,
            studentId,
            cancellationToken);

        if (!decision.IsAllowed)
        {
            denialAuditor.AuditDenied(decision, "class", activeClassId.Value.ToString());
            return hiddenResourceResponseFactory.FromDecision(decision);
        }

        var classContext = await classService.GetClassContextByIdAsync(activeClassId.Value, cancellationToken);
        if (classContext is null
            || !string.Equals(classContext.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            var notFoundDecision = AuthorizationDecision.HiddenNotFound(
                "classes.notFound",
                classContext is null
                    ? AuthorizationDenialReason.ClassNotFound
                    : AuthorizationDenialReason.ClassMembership);
            denialAuditor.AuditDenied(notFoundDecision, "class", activeClassId.Value.ToString());
            return hiddenResourceResponseFactory.FromDecision(notFoundDecision);
        }

        return Ok(new ClassCurrentResponse(
            classContext.ClassId,
            classContext.ClassName,
            classContext.ClassCode,
            classContext.Status));
    }

    [Authorize(Roles = IdentityRoleNames.Teacher)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetClassDetail(Guid id, CancellationToken cancellationToken)
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
            AuthorizationPolicies.CanViewClassAsTeacher);

        var decision = await classAuthorizationService.RequireTeacherClassAccessAsync(
            id,
            teacherId,
            cancellationToken);

        if (!authorizationResult.Succeeded || !decision.IsAllowed)
        {
            denialAuditor.AuditDenied(decision, "class", id.ToString());
            return hiddenResourceResponseFactory.FromDecision(decision);
        }

        var result = await classService.GetClassDetailForTeacherAsync(id, teacherId, cancellationToken);
        if (!result.Allowed || result.Detail is null)
        {
            var fallbackDecision = AuthorizationDecision.HiddenNotFound(
                result.ErrorCode ?? "classes.notFound",
                AuthorizationDenialReason.ClassOwnership);
            denialAuditor.AuditDenied(fallbackDecision, "class", id.ToString());
            return hiddenResourceResponseFactory.FromDecision(fallbackDecision);
        }

        return Ok(result.Detail);
    }
}
