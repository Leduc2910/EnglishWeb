using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Persistence;

public sealed class EnglishTestWebDbContext(DbContextOptions<EnglishTestWebDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityRole>().HasData(IdentityRoleDefinitions.All);
    }
}
