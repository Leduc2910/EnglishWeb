using System.Security.Claims;
using EnglishTestWeb.Api.Application.Auth;
using EnglishTestWeb.Api.Contracts.Auth;
using EnglishTestWeb.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class AuthService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    public async Task<AuthLoginResult> LoginTeacherAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var identifier = request.Identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        var user = await FindUserAsync(identifier);
        if (user is null)
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        if (!await userManager.IsInRoleAsync(user, IdentityRoleNames.Teacher))
        {
            await signInManager.SignOutAsync();
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        await signInManager.SignInAsync(user, isPersistent: request.RememberMe);

        return new AuthLoginResult(true, await MapUserAsync(user), null);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        return await MapUserAsync(user);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return signInManager.SignOutAsync();
    }

    public async Task<AuthLoginResult> SignInForTestingAsync(
        TestingSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        var signInResult = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        return new AuthLoginResult(true, await MapUserAsync(user), null);
    }

    private async Task<ApplicationUser?> FindUserAsync(string identifier)
    {
        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            return await userManager.FindByEmailAsync(identifier);
        }

        return await userManager.FindByNameAsync(identifier);
    }

    private async Task<CurrentUserResponse> MapUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.UserName,
            roles.ToList());
    }
}
