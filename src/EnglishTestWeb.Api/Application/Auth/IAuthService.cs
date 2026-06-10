using System.Security.Claims;
using EnglishTestWeb.Api.Contracts.Auth;

namespace EnglishTestWeb.Api.Application.Auth;

public interface IAuthService
{
    Task<AuthLoginResult> LoginTeacherAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<AuthLoginResult> SignInForTestingAsync(
        TestingSignInRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AuthLoginResult(bool Succeeded, CurrentUserResponse? User, string? ErrorCode);
