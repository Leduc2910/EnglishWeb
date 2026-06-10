namespace EnglishTestWeb.Api.Infrastructure.Audit;

public interface IAuthorizationAuditLogger
{
    void LogDenied(
        string? actorId,
        string? role,
        string resourceType,
        string? resourceId,
        string reasonCategory,
        string? correlationId);
}
