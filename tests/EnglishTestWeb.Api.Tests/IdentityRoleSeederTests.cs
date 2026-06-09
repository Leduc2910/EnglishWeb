using EnglishTestWeb.Api.Application.Identity;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests;

public sealed class IdentityRoleSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotentForTeacherAndStudentRoles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<EnglishTestWebDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<EnglishTestWebDbContext>();
        services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>();
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var roleNames = await dbContext.Roles
            .OrderBy(role => role.Name)
            .Select(role => role.Name)
            .ToListAsync();

        Assert.Equal([IdentityRoleNames.Student, IdentityRoleNames.Teacher], roleNames);
        Assert.Equal(2, await dbContext.Roles.CountAsync());
    }
}
