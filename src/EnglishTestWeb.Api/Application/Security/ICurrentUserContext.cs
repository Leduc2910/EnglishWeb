namespace EnglishTestWeb.Api.Application.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    IReadOnlyList<string> Roles { get; }

    Guid? ActiveClassId { get; }

    bool IsInRole(string role);
}
