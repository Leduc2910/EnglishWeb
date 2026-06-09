using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishTestWeb.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), "EnglishTestWeb.Api.Tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=EnglishTestWeb_Unused;Trusted_Connection=True;TrustServerCertificate=True",
                ["DataProtection:KeysPath"] = Path.Combine(_testRoot, "data-protection-keys"),
                ["ProtectedStorage:RootPath"] = Path.Combine(_testRoot, "protected-storage")
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EnglishTestWebDbContext>>();
            services.AddDbContext<EnglishTestWebDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
