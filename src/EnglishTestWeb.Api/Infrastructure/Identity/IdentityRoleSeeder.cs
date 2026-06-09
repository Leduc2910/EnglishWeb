using EnglishTestWeb.Api.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class IdentityRoleSeeder(RoleManager<IdentityRole> roleManager) : IIdentityRoleSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in IdentityRoleNames.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
                throw new InvalidOperationException($"Failed to seed role '{roleName}'. {errors}");
            }
        }
    }
}
