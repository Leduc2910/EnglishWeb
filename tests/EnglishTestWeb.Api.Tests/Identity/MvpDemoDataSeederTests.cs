using EnglishTestWeb.Api.Application.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Identity;

public sealed class MvpDemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var factory = new TestApiFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>().SeedAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<IMvpDemoDataSeeder>();
            await seeder.SeedAsync();
            await seeder.SeedAsync();
        }

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var classCount = await verifyDbContext.Classes.CountAsync(entity => entity.ClassCode == "ENG7A");
        Assert.Equal(1, classCount);

        var membershipCount = await verifyDbContext.ClassMemberships.CountAsync();
        Assert.Equal(1, membershipCount);
    }
}
