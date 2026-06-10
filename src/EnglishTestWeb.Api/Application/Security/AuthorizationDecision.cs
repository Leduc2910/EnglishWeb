namespace EnglishTestWeb.Api.Application.Security;

public enum AuthorizationOutcome
{
    Allowed,
    HiddenNotFound,
    Forbidden
}

public sealed record AuthorizationDecision(
    AuthorizationOutcome Outcome,
    string? ErrorCode,
    string? DenialReason)
{
    public bool IsAllowed => Outcome == AuthorizationOutcome.Allowed;

    public static AuthorizationDecision Allow() =>
        new(AuthorizationOutcome.Allowed, null, null);

    public static AuthorizationDecision HiddenNotFound(string errorCode, string denialReason) =>
        new(AuthorizationOutcome.HiddenNotFound, errorCode, denialReason);

    public static AuthorizationDecision Forbidden(string errorCode, string denialReason) =>
        new(AuthorizationOutcome.Forbidden, errorCode, denialReason);
}
