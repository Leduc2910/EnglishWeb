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

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "2eb4f724-2f8a-42a2-b65a-590e1885b5f1",
                Name = IdentityRoleNames.Teacher,
                NormalizedName = IdentityRoleNames.Teacher.ToUpperInvariant(),
                ConcurrencyStamp = "f3b8d154-8ce3-42e2-8078-f4c3c5ac8185"
            },
            new IdentityRole
            {
                Id = "5a298208-ed57-42e6-8a55-6bdc2b9efa22",
                Name = IdentityRoleNames.Student,
                NormalizedName = IdentityRoleNames.Student.ToUpperInvariant(),
                ConcurrencyStamp = "1f028db7-7907-4afa-afb8-d8c9843ca9c7"
            });
    }
}
