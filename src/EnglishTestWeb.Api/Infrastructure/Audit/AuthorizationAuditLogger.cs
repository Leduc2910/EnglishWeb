namespace EnglishTestWeb.Api.Infrastructure.Audit;

public sealed class AuthorizationAuditLogger(ILogger<AuthorizationAuditLogger> logger) : IAuthorizationAuditLogger
{
    public void LogDenied(
        string? actorId,
        string? role,
        string resourceType,
        string? resourceId,
        string reasonCategory,
        string? correlationId)
    {
        logger.LogInformation(
            "authorization.denied ActorId={ActorId} Role={Role} ResourceType={ResourceType} ResourceId={ResourceId} ReasonCategory={ReasonCategory} CorrelationId={CorrelationId}",
            actorId ?? "anonymous",
            role ?? "none",
            resourceType,
            resourceId ?? "none",
            reasonCategory,
            correlationId ?? "none");
    }
}
