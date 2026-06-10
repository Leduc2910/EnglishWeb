using EnglishTestWeb.Api.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class IdentityRoleSeeder(RoleManager<IdentityRole> roleManager) : IIdentityRoleSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleDefinition in IdentityRoleDefinitions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleDefinition.Name!))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole
            {
                Id = roleDefinition.Id,
                Name = roleDefinition.Name,
                NormalizedName = roleDefinition.NormalizedName,
                ConcurrencyStamp = roleDefinition.ConcurrencyStamp
            });
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
                throw new InvalidOperationException($"Failed to seed role '{roleDefinition.Name}'. {errors}");
            }
        }
    }
}
