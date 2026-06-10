using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Audit;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Infrastructure.Authorization;

public sealed class AuthorizationDenialAuditor(
    IAuthorizationAuditLogger auditLogger,
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserContext currentUserContext)
{
    public void AuditDenied(AuthorizationDecision decision, string resourceType, string? resourceId)
    {
        if (decision.IsAllowed || string.IsNullOrWhiteSpace(decision.DenialReason))
        {
            return;
        }

        var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var role = currentUserContext.Roles.FirstOrDefault();

        auditLogger.LogDenied(
            currentUserContext.UserId,
            role,
            resourceType,
            resourceId,
            decision.DenialReason,
            correlationId);
    }
}
