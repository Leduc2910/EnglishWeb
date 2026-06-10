using System.Security.Claims;
using EnglishTestWeb.Api.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using EnglishTestWeb.Api.Application.Classes;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.Auth;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class AuthService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IClassService classService,
    IClassAuthorizationService classAuthorizationService) : IAuthService
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

        return new AuthLoginResult(true, await MapUserAsync(user, principal: null, cancellationToken), null);
    }

    public async Task<StudentLoginResult> LoginStudentAsync(
        StudentLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var classContext = await classService.GetActiveClassByCodeAsync(request.ClassCode, cancellationToken);
        if (classContext is null)
        {
            return new StudentLoginResult(false, null, "classes.codeNotFound");
        }

        if (!string.Equals(classContext.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            return new StudentLoginResult(false, null, "classes.codeInactive");
        }

        var identifier = request.Identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new StudentLoginResult(false, null, "auth.loginInvalid");
        }

        var user = await FindUserAsync(identifier);
        if (user is null)
        {
            return new StudentLoginResult(false, null, "auth.loginInvalid");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return new StudentLoginResult(false, null, "auth.loginInvalid");
        }

        if (!await userManager.IsInRoleAsync(user, IdentityRoleNames.Student))
        {
            await signInManager.SignOutAsync();
            return new StudentLoginResult(false, null, "auth.loginInvalid");
        }

        if (!await classService.HasActiveMembershipAsync(classContext.ClassId, user.Id, cancellationToken))
        {
            await signInManager.SignOutAsync();
            return new StudentLoginResult(false, null, "auth.loginInvalid");
        }

        var classClaim = new Claim(AuthorizationClaimTypes.ActiveClassId, classContext.ClassId.ToString());
        await signInManager.SignInWithClaimsAsync(
            user,
            new AuthenticationProperties { IsPersistent = request.RememberMe },
            [classClaim]);

        var mappedUser = await MapUserAsync(user, principal: null, cancellationToken);
        var activeClass = new ActiveClassResponse(
            classContext.ClassId,
            classContext.ClassName,
            classContext.ClassCode);

        return new StudentLoginResult(
            true,
            new StudentLoginResponse(
                mappedUser.UserId,
                mappedUser.Email,
                mappedUser.UserName,
                mappedUser.Roles,
                activeClass),
            null);
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

        return await MapUserAsync(user, principal, cancellationToken);
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

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return new AuthLoginResult(false, null, "auth.loginInvalid");
        }

        var extraClaims = new List<Claim>();
        if (request.ActiveClassId.HasValue)
        {
            extraClaims.Add(new Claim(
                AuthorizationClaimTypes.ActiveClassId,
                request.ActiveClassId.Value.ToString()));
        }

        if (extraClaims.Count > 0)
        {
            await signInManager.SignInWithClaimsAsync(user, isPersistent: false, extraClaims);
        }
        else
        {
            await signInManager.SignInAsync(user, isPersistent: false);
        }

        var principal = await CreatePrincipalForUserAsync(user, extraClaims);
        return new AuthLoginResult(true, await MapUserAsync(user, principal, cancellationToken), null);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalForUserAsync(
        ApplicationUser user,
        IReadOnlyList<Claim> extraClaims)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        if (extraClaims.Count == 0)
        {
            return principal;
        }

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return principal;
        }

        foreach (var claim in extraClaims)
        {
            identity.AddClaim(claim);
        }

        return principal;
    }

    private async Task<ApplicationUser?> FindUserAsync(string identifier)
    {
        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            return await userManager.FindByEmailAsync(identifier);
        }

        return await userManager.FindByNameAsync(identifier);
    }

    private async Task<CurrentUserResponse> MapUserAsync(
        ApplicationUser user,
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        ActiveClassResponse? activeClass = null;

        if (roles.Contains(IdentityRoleNames.Student))
        {
            var classIdValue = principal?.FindFirstValue(AuthorizationClaimTypes.ActiveClassId);
            if (Guid.TryParse(classIdValue, out var classId))
            {
                var decision = await classAuthorizationService.RequireStudentClassAccessAsync(
                    classId,
                    user.Id,
                    cancellationToken);

                if (decision.IsAllowed)
                {
                    var classContext = await classService.GetClassContextByIdAsync(classId, cancellationToken);
                    if (classContext is not null
                        && string.Equals(classContext.Status, ClassStatuses.Active, StringComparison.Ordinal))
                    {
                        activeClass = new ActiveClassResponse(
                            classContext.ClassId,
                            classContext.ClassName,
                            classContext.ClassCode);
                    }
                }
            }
        }

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.UserName,
            roles.ToList(),
            activeClass);
    }
}
