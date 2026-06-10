using System.Security.Claims;
using EnglishTestWeb.Api.Application.Security;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Infrastructure.Authorization;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
        ?? [];

    public Guid? ActiveClassId
    {
        get
        {
            var value = Principal?.FindFirstValue(AuthorizationClaimTypes.ActiveClassId);
            return Guid.TryParse(value, out var classId) ? classId : null;
        }
    }

    public bool IsInRole(string role) =>
        Principal?.IsInRole(role) ?? false;
}
