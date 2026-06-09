namespace EnglishTestWeb.Api.Application.Identity;

public interface IIdentityRoleSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
