using EnglishTestWeb.Api.Infrastructure.Audit;

namespace EnglishTestWeb.Api.Tests.Security;

public sealed class FakeAuthorizationAuditLogger : IAuthorizationAuditLogger
{
    public List<AuthorizationAuditRecord> Records { get; } = [];

    public void LogDenied(
        string? actorId,
        string? role,
        string resourceType,
        string? resourceId,
        string reasonCategory,
        string? correlationId)
    {
        Records.Add(new AuthorizationAuditRecord(
            actorId,
            role,
            resourceType,
            resourceId,
            reasonCategory,
            correlationId));
    }
}

public sealed record AuthorizationAuditRecord(
    string? ActorId,
    string? Role,
    string ResourceType,
    string? ResourceId,
    string ReasonCategory,
    string? CorrelationId);
