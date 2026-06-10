using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Classes;

internal static class ClassesTestHelper
{
    internal const string ClassCode = "ENG7A";
    internal const string ClassName = "English 7A";
    internal const string OtherTeacherEmail = "other-teacher@test.local";
    internal const string OtherTeacherPassword = "Teacher123!";
    internal const string NonMemberStudentEmail = "other-student@test.local";
    internal const string NonMemberStudentPassword = "Student123!";

    internal static async Task SeedDemoClassAsync(TestApiFactory factory)
    {
        await Auth.AuthTestHelper.SeedRolesAndUsersAsync(factory);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");
        var student = await userManager.FindByEmailAsync(Auth.AuthTestHelper.StudentEmail)
            ?? throw new InvalidOperationException("Student user missing.");

        var schoolClass = await dbContext.Classes.FirstOrDefaultAsync(entity => entity.ClassCode == ClassCode);
        if (schoolClass is null)
        {
            schoolClass = new SchoolClass
            {
                Id = Guid.NewGuid(),
                Name = ClassName,
                ClassCode = ClassCode,
                TeacherId = teacher.Id,
                Status = ClassStatuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Classes.Add(schoolClass);
            await dbContext.SaveChangesAsync();
        }

        var membership = await dbContext.ClassMemberships.FirstOrDefaultAsync(entry =>
            entry.ClassId == schoolClass.Id && entry.StudentId == student.Id);
        if (membership is null)
        {
            dbContext.ClassMemberships.Add(new ClassMembership
            {
                Id = Guid.NewGuid(),
                ClassId = schoolClass.Id,
                StudentId = student.Id,
                Status = ClassStatuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        await EnsureUserAsync(userManager, OtherTeacherEmail, OtherTeacherPassword, IdentityRoleNames.Teacher);
        await EnsureUserAsync(userManager, NonMemberStudentEmail, NonMemberStudentPassword, IdentityRoleNames.Student);
    }

    internal static async Task<Guid> SeedInactiveClassAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");

        var schoolClass = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Class",
            ClassCode = "INACTIVE1",
            TeacherId = teacher.Id,
            Status = ClassStatuses.Inactive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Classes.Add(schoolClass);
        await dbContext.SaveChangesAsync();
        return schoolClass.Id;
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email.Split('@')[0],
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user '{email}'.");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
