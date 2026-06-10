namespace EnglishTestWeb.Api.Application.Security;

public static class AuthorizationDenialReason
{
    public const string Unauthenticated = "unauthenticated";
    public const string WrongRole = "wrongRole";
    public const string ClassOwnership = "class.ownership";
    public const string ClassMembership = "class.membership";
    public const string ClassNotFound = "class.notFound";
}
