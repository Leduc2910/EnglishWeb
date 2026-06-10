using EnglishTestWeb.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class IdentityDevUserOptions
{
    public const string SectionName = "Identity:DevTeacher";

    public string Email { get; set; } = "teacher@englishtestweb.local";

    public string Password { get; set; } = "Teacher123!";

    public string UserName { get; set; } = "teacher";
}

public interface IIdentityDevUserSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class IdentityDevUserSeeder(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityDevUserOptions> options) : IIdentityDevUserSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var email = settings.Email.Trim();
        var userName = settings.UserName.Trim();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, IdentityRoleNames.Teacher))
            {
                await userManager.AddToRoleAsync(existing, IdentityRoleNames.Teacher);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, settings.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to seed dev teacher user '{email}'. {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, IdentityRoleNames.Teacher);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to assign Teacher role to dev user '{email}'. {errors}");
        }
    }
}
