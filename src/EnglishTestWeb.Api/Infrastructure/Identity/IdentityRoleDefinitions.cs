using Microsoft.AspNetCore.Identity;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public static class IdentityRoleDefinitions
{
    public static readonly IdentityRole Teacher = new()
    {
        Id = "2eb4f724-2f8a-42a2-b65a-590e1885b5f1",
        Name = IdentityRoleNames.Teacher,
        NormalizedName = IdentityRoleNames.Teacher.ToUpperInvariant(),
        ConcurrencyStamp = "f3b8d154-8ce3-42e2-8078-f4c3c5ac8185"
    };

    public static readonly IdentityRole Student = new()
    {
        Id = "5a298208-ed57-42e6-8a55-6bdc2b9efa22",
        Name = IdentityRoleNames.Student,
        NormalizedName = IdentityRoleNames.Student.ToUpperInvariant(),
        ConcurrencyStamp = "1f028db7-7907-4afa-afb8-d8c9843ca9c7"
    };

    public static readonly IdentityRole[] All = [Teacher, Student];
}
