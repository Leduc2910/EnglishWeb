using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnglishTestWeb.Api.Infrastructure.Persistence;

public sealed class DesignTimeEnglishTestWebDbContextFactory : IDesignTimeDbContextFactory<EnglishTestWebDbContext>
{
    public EnglishTestWebDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EnglishTestWebDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EnglishTestWeb_DesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new EnglishTestWebDbContext(options);
    }
}
